# TPML Windows IME 修复实施计划

## 1. 问题与目标

通过 `tPlainModLoader.exe` 启动 Terraria 后，原版角色名、世界名、聊天等任意文本输入场景仍能接收普通键盘字符，但 Windows 中文输入法无法进入拼音合成状态，也不会提交中文候选。

目标是恢复与直接启动纯净 `Terraria.exe` 相同的 Windows IME 行为，不替换 Terraria 原生输入框、`WindowsIme`、候选框绘制或按键解析实现。

## 2. 已确认现象与已排除方向

1. 原版入口 `Terraria.WindowsLaunch.Main(string[])` 标记了 `[STAThread]`。
2. 直接启动 `Terraria.exe` 时，CLR 会根据入口特性将游戏主线程初始化为 STA。
3. TPML 的 `LaunchGame.Run(...)` 通过 `Task.Run(...)` 反射调用该入口。方法上的 `[STAThread]` 在普通反射调用时不会改变当前线程的单元状态，而 .NET Framework 线程池线程默认为 MTA。
4. Terraria 在游戏线程上创建 XNA 窗口，并在 `Main.ClientInitialize()` 中通过 `Platform.Current.InitializeClientServices(Window.Handle)` 创建 `WindowsMessageHook` 和 `WindowsIme`。
5. TPML 原实现确实把游戏窗口、WinForms 消息过滤器和 Windows IME 服务放在 MTA 线程上，偏离了原版入口的 STA 线程环境。

已通过独立的内存内 C# 探针验证线程单元行为：

```text
TaskRun         : MTA
DedicatedThread : STA
```

探针调用同一个带 `[STAThread]` 的方法；结果证明该特性不会在 `Task.Run` 的普通方法调用中自动切换线程单元。现已改为专用 STA 线程，运行日志也确认游戏线程为 `STA`，但用户实机复测后中文输入法仍不能使用。因此 STA 是已修正的启动语义偏差，不是完整根因。

`tContentPatch.Content.DrawIME` 是旧的候选框绘制补丁。当前活动源码没有任何位置把 `NeedIME` 设为 `true`，而原版 `Main.DoDraw()` 已自行调用 `DrawIMEPanel()`；它不接收 `WM_IME_*`、不初始化 IME、也不提交字符，因此不是所有原版输入框失效的直接根因。

Terraria 内嵌的 `ReLogic.dll` 与 TPML 部署版本一致，Terraria 根目录和 TPML 目录的 `ReLogic.Native.dll` 也一致，已排除 ReLogic 托管/原生程序集版本漂移。

## 3. 诊断与正式修复

诊断阶段保留已完成的专用 STA 游戏线程改动，并在 `tContentPatch` 增加临时 `[IME-DIAG]` 运行态日志：

1. 记录 `PlayerInput.WritingText`、`Main._imeToggle`、`IImeService` 实际类型和托管启用状态。
2. 记录 `ReLogic.Native.dll` 的 IME 启用状态、组合串、候选框状态、游戏 HWND、前台 HWND、IMM 上下文和输入法开关。
3. 记录托管/Win32 线程 ID、窗口所属线程、ApartmentState、键盘布局和 `Main.keyCount`。
4. 观察 `Main.HandleIME()` 与 `PlatformIme.Enable()/Disable()` 的前后状态。
5. 观察 `WindowsIme.PreFilterMessage(ref Message)` 收到的焦点、输入语言、键盘、字符和 `WM_IME_*` 消息及处理结果。
6. 诊断代码不拦截、不改写消息；轮询只在状态变化时记录，文本输入期间每 2 秒补一条心跳。

真实键盘日志已经确认：

1. 输入场景中 `PlayerInput.WritingText=true`，`Main.HandleIME()` 正常调用 `WindowsIme.Enable()`。
2. 托管 `IsEnabled=true`，但 `ReLogic.Native.ImeUi_IsEnabled()` 始终为 `false`。
3. 游戏窗口为前台窗口，键盘布局为中文 `0x8040804`，但窗口没有关联 HIMC。
4. 窗口只收到 ASCII `WM_CHAR`，没有 `WM_IME_STARTCOMPOSITION` 或 `WM_IME_COMPOSITION`。
5. `ReLogic.Native.ImeUi_Initialize()` 在首次 `ImmGetContext(hwnd)` 返回空时会进入永久禁用分支，后续 `Enable(true)` 无法恢复。

因此正式修复放在 `Main.ClientInitialize()` 内的 `Platform.Current.InitializeClientServices(Window.Handle)` 之前：

1. 先尝试通过 `ImmAssociateContextEx(..., IACE_DEFAULT)` 恢复窗口的线程默认输入上下文。
2. 若默认上下文仍不可用，再通过 `ImmCreateContext()` 创建并关联新的 HIMC。
3. 随后保持 ReLogic 原生初始化、启停、候选框与字符提交逻辑不变。
4. Prepatcher 在原调用前插入 `dup + call ImeContextBootstrap.EnsureAssociated(hwnd)`，不改变 `InitializeClientServices` 的原参数栈。

第一次真实键盘复测曾通过，日志确认启动期恢复默认 HIMC，输入期原生 IME 成功启用，候选框、组合串、合成开始/更新/结束消息均正常。但移除 `ImeDiagnostics` Harmony 观察器并重新构建后，用户再次启动即复现输入法失效，说明观察器存在尚未识别的行为或时序影响，不能按纯诊断代码删除。当前已恢复该观察器与初始化调用，等待真实键盘再次复测并根据新日志继续收敛。

## 4. 验证步骤

1. 运行全量 Release 构建：

   `dotnet build "tPlainModLoader\tPlainModLoader\tPlainModLoader.sln" -c Release -m /graph`

2. 核对构建产物与自动部署后的 `tContentPatch.dll` SHA-256 一致。
3. 用 TPML 启动 Terraria，进入原版文本输入场景。
4. 使用人工真实键盘输入，而不是脚本文本注入、剪贴板或 GABS 字段赋值：
   - 普通英文字符可输入；
   - Windows 中文输入法可切换；
   - 拼音按键产生合成串或候选框；
   - 选择候选后中文字符进入输入框；
   - 至少连续输入几次拼音，并使用空格尝试选词。
5. 退出游戏后读取 `C:\Games\Steam\steamapps\common\Terraria\tPlainModLoader\tpml.log`：
   - 启动期应出现成功的 `[IME-BOOTSTRAP]` 日志；
   - 输入期 `nativeEnabled` 应变为 `true`；
   - 应出现 `WM_IME_STARTCOMPOSITION`、`WM_IME_COMPOSITION` 和非空组合串；
   - 选词后中文字符应进入原版输入框。

## 5. 完成标准

- Release 全量构建无错误；
- 已获得真实键盘复现与修复后的完整 `[IME-DIAG]` 状态链；
- 已确认故障位于 ReLogic.Native 初始化前缺少窗口 HIMC；
- 已实施 HIMC 初始化修复，并保留会影响复现结果的 IME 观察器；
- TPML 下真实键盘中文输入在当前最终构建中连续复测通过；
- 普通英文输入和现有 TPML 加载流程无回归；
- 将最终改动、验证结果和任何偏差记录到 `tPlainModLoader/WALKTHROUGH.md`。
