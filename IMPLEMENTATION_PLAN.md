# TPML Windows IME 修复实施计划

## 1. 问题与目标

通过 `tPlainModLoader.exe` 启动 Terraria 后，原版角色名、世界名、聊天等文本输入场景能接收普通英文字符，但 Windows 中文输入法无法进入拼音合成状态，也不会弹出候选框。

目标是在不替换 Terraria 原生输入框、`WindowsIme`、候选框绘制或按键解析实现的前提下，恢复与官方原版 `Terraria.exe` 完全一致的 Windows IME 中文输入能力。

---

## 2. 根因分析与机制确认

1. **游戏主线程单元（STA）**：
   - 官方原版 `Terraria.WindowsLaunch.Main` 标记了 `[STAThread]`。TPML 改用专用 STA 游戏线程启动，确保符合 Windows OLE/IME 单元规范。
2. **初始化时缺少窗口 HIMC（核心根因）**：
   - TPML 创建 XNA/WinForms 窗口初期，窗口尚未绑定默认 IMM 输入法上下文（`ImmGetContext(hwnd) == 0`）；
   - 在 `Main.ClientInitialize()` 中调用 `Platform.InitializeClientServices(Window.Handle)` 时，底层 `ReLogic.Native!ImeUi_Initialize(HWND)` 会调用 `ImmGetContext(hwnd)`；
   - 若返回空，原生库会设置**永久失效标记**。之后即使调用 `WindowsIme.Enable()`，`ImeUi_EnableIme(true)` 也会被强制忽略。
3. **ReLogic 原生启停状态机**：
   - 初始化成功后，ReLogic 会主动执行 `ImeUi_EnableIme(false)`（通过 `ImmAssociateContext(hwnd, NULL)` 解除关联），这是 ReLogic 的预期空闲状态；
   - 当进入文本输入框时，Terraria 调用 `PlatformIme.Enable()` -> `WindowsIme.OnEnable()` -> `ImeUi_EnableIme(true)`，由原生库自动将初始化时保存的 HIMC 重新挂回游戏窗口；
   - 原版状态机具备完整的自洽生命周期，无需任何运行时 Harmony Hook 拦截。

---

## 3. 正式修复方案

1. **`ImeContextBootstrap` 预初始化恢复**：
   - 在 `Main.ClientInitialize()` 的 `Platform.InitializeClientServices(HWND)` 调用前，通过 Prepatcher 织入 `dup + call ImeContextBootstrap.EnsureAssociated(hwnd)`；
   - 优先通过 `ImmAssociateContextEx(hwnd, IntPtr.Zero, IACE_DEFAULT)` 恢复窗口的系统默认输入上下文；若失败则通过 `ImmCreateContext()` 创建并绑定新上下文；
   - 确保 `ReLogic.Native!ImeUi_Initialize` 首次执行时读取到有效 HIMC 并成功初始化。
2. **零运行时 Hook 与纯净交付**：
   - 彻底删除全部临时运行态诊断监听与轮询代码（`ImeDiagnostics.cs`，570+ 行）；
   - 不在运行时拦截 `PlatformIme.Enable/Disable` 或 `WindowsIme.PreFilterMessage`，杜绝一切潜在的 JIT 权限异常与消息时序干扰。

---

## 4. 验证与完成标准

- **全量构建**：Release 全量构建 20 个工程全部通过（0 警告，0 错误）；
- **实机复测**：真实键盘在角色命名、世界命名及聊天框中均能正常呼出微软拼音/第三方输入法，候选词正常选入；
- **日志校验**：启动期 `[IME-BOOTSTRAP]` 成功关联 HIMC，无任何异常报错；
- **文档一致性**：更新 `WALKTHROUGH.md` 并同步父仓库子模块指针。
