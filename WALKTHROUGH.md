# tPlainModLoader (TPML) 技术演进与架构落地 Walkthrough

> **项目来源**：Fork 自 [github-user-64/tPlainModLoader](https://github.com/github-user-64/tPlainModLoader)  
> **维护与重构**：`SaintCirno9`  
> **适用环境**：.NET Framework 4.7.2 (x86) / Terraria 1.4.4+ & 1.4.5+  

---

## 1. 项目定位与架构演进总览

`tPlainModLoader` (TPML) 是面向官方原版 Terraria (`Terraria.exe`) 的轻量级原生模组加载器、公有化预处理与底层补丁框架。

本项目在原作者 `github-user-64` 的轻量加载器原型基础上，进行了全方位的现代化重构与生态扩展，主要解决以下核心诉求：
1. **100% 原版游戏与存档兼容**：直接基于官方原版 Terraria 客户端启动，使用原生 `.plr` / `.wld` 存档格式，无需独立庞大的 Modded 运行环境；
2. **消灭反射（Publicizer 零反射基础设施）**：在编译期和启动运行期全面实现成员公开化，彻底摆脱低效脆弱的反射代码，享受 100% 强类型语法与编译器静态检查；
3. **原生性能级字段挂载（Prepatcher 动态注入）**：通过 Mono.Cecil 在运行时直接向原版类注入字段，替代高开销的弱引用与哈希字典；
4. **原生标准统一体验（按键、内容与配置）**：提供无缝融入原版设置界面的统一按键框架、标准原生内容引擎 `TPML.Content` 以及集约化模组配置体系；
5. **开箱即用的实用模组矩阵**：提供涵盖全方位 QoL、建筑魔杖、大背包、连锁挖矿、高刷鼠标优化与直通车在内的 14+ 原生模组。

---

## 2. 核心底层基础设施里程碑

### 2.1 SDK 风格与 MSBuild 现代工程架构
- **全量 SDK 风格改造**：包括加载器宿主、注入器、`tContentPatch`、`TPML.Content` 以及全部 14 个模组在内的所有工程，全面转换为现代 SDK 风格 csproj（`net472` 目标框架）；
- **全量并发构建优化**：启用 MSBuild 静态图拓扑评估与多核并发编译（`dotnet build ... -c Release -m /graph`），全解决方案 20+ 个工程通常在 4 秒内完成全量构建；
- **4GB 虚拟内存感知 (LargeAddressAware)**：构建流水线内置 `RoslynCodeTaskFactory` 任务，自动为生成的 PE 二进制注入 `IMAGE_FILE_LARGE_ADDRESS_AWARE` (0x0020) 标志，将 32 位 CLR 用户空间上限由 2GB 提升至 4GB，根治多模组环境下的 OOM 崩溃；
- **自动化增量热部署 (`DeployToGameDir`)**：编译后通过 MSBuild 增量复制任务自动将程序集、PDB 与元数据部署至游戏安装目录 `$(TerrariaDir)\tPlainModLoader\`。

### 2.2 内存公有化 (Publicizer) 与强类型零反射
- **编译期公有化**：在 `Directory.Build.props` 中全局引入 `Krafs.Publicizer`，为 `Terraria` 与 `ReLogic` 自动生成公有化引用程序集（排除编译器事件字段与保持虚方法修饰符，避免二义性冲突）；
- **运行期 Cecil 动态公开化**：在 `LaunchGame` 阶段通过 `Mono.Cecil` 在内存中遍历 `Terraria.exe` 的所有类型、字段、属性与方法定义，将其访问修饰符统一改写为 `Public` 后载入 AppDomain；
- **全生态消灭反射**：所有模组与底层补丁代码彻底废除 `FieldInfo`、`MethodInfo` 与 `AccessTools` 等运行时反射操作，全部改为原生强类型直连访问。

### 2.3 Prepatcher 动态字段注入与早期 Cecil 预修补
- **自由字段注入 (Free Fields)**：通过 `[PrepatcherField]` 特性声明扩展方法，Prepatcher 引擎在启动期直接向目标原版类（如 `Player`、`NPC`、`Item`）注入原生实例字段，并将方法体清空改写为单条原生 IL 访问指令（`ldarg.0` + `ldflda` / `ldfld` + `ret`），实现零哈希查找、零 GC 损耗的极限访问性能；
- **早期 Cecil 预补丁 (IPrepatcher / FreePatch)**：支持模组在程序集被 CLR 载入前，通过 `IPrepatcher` 接口对游戏字节码进行底层类型改写、常量重写与早期 IL 织入。

### 2.4 原生级统一按键总线 (KeybindLoader)
- **无缝集成原版控件菜单**：模组通过 `KeybindLoader.RegisterKeybind` 注册的自定义快捷键会自动注入到原版【控件 (Controls)】设置面板中，玩家可自由查看和重新绑定按键；
- **状态机与打字冲突防误触**：支持 `JustPressed`、`Current`、`JustReleased` 边沿状态判定；在玩家打开聊天框、编辑告示牌或输入文本时自动全局静默，杜绝按键冲突。

### 2.5 原生内容加载引擎 (TPML.Content)
- **解耦独立架构**：彻底清理历史遗留的 Shim 兼容垫片代码，构建纯净的 `TPML.Content` 原生内容命名空间；
- **生命周期与配方独立管理**：提供 `ModItem`、`ModSystem`、`ItemLoader` 与 `RecipeLoader` 等组件，支持自定义物品注册、多语言 Tooltip 动态拼装、向导合成查询、创造模式检索与独立配方构建；
- **动态 Hook 调度**：`ContentHookDispatcher` 负责拦截物品使用、弹幕射击、手持状态、图层渲染与界面生命周期。

---

## 3. 模组生态与功能整编

### 3.1 旗舰整合模组 `OptimizeAndTool`
将原本细碎分散的多个独立模组深度整编为统一的旗舰级 QoL 与优化模组：
- **规则与自动化 QoL**：物品 9999 堆叠、随身便携制作站（直连背包与四箱）、无尽增益药水与随身增益站/旗帜、NPC 自动入住与最低折扣、渔夫无冷却任务刷新；
- **扩展存储系统**：巨大随身背包（`BigBag`，支持 40~500 格容量、Shift 智能互存与便携制作联动）以及独立饰品收纳箱（`AccessoryBox`）；
- **采矿与建造辅助**：简单连锁挖矿（`VeinMining`，基于 BFS 递归采掘矿石与宝石）与智能吸管工具（`Pipette`，一键识别并手持对应图格/墙体）；
- **性能与视口**：高刷鼠标硬件即时采样、视口缩放限制放宽与光照遍历优化；
- **创造与调试**：创造模式物品栏（全物品分类检索与批量提取）、上帝模式无敌、全图透视点亮与环境检测。

### 3.2 建筑与蓝图套件 `WandsTool`
- **几何塑形**：支持直线、圆形/椭圆、实心/空心矩形、半砖与 4 向斜坡智能平滑；
- **大范围破坏与液体**：支持法爆/星爆区域批量挖掘，以及水/岩浆/蜂蜜/微光的吸收、清空与无限放置；
- **建筑结构蓝图系统**：框选建筑结构一键导出为 JSON 蓝图，支持水平镜像（H）、垂直翻转（V）、半透明虚影预览、材料自动代工计算与跨存档复制粘贴。

### 3.3 原生内容模组 `Instavator` (地狱直通车)
- **垂直矿井分帧建造**：一键安全开凿直通地狱底层的垂直矿井，自动铺设中心绳索、火把照明与黑曜石砖护壁，具备不可破坏区域保护与液体排净功能；
- **实时手持选区预览**：手持地狱直通车时，界面实时以半透明高亮方框展示挖掘范围与深度，根据当前手持规格自动计算终点深度，画面稳定无频闪；
- **质量扫描与建造快照**：内置建造指标快照记录，并支持切片式物理扫描计算直通率、绳索连续性与护壁完整率。

### 3.4 基础与专业模组矩阵
- **`QuickSetting`**：抽屉式游戏内即时配置中心，支持模组动态注册开关、滑块与按键设置；
- **`QuickButton`**：可自由拖拽、折叠停靠的屏幕悬浮快捷工具栏；
- **`PixelArt`**：本地 PNG/JPG 图片色彩映射与方块像素画快速摆放器；
- **`ChatAi`**：将大语言模型接入游戏内聊天频道，提供即时问答与攻略助手；
- **`Skil`**：提供 15+ 种主动战斗与魔法技能；
- **`SuspiciousPlayer`**：服务端原生兼容的多阶段史莱姆枪 BOSS 战挑战。

---

## 4. 体验治理与稳定性深度排查

### 4.1 启动黑屏与白屏闪烁根除
- **根因**：XNA 在调用 `main.Run()` 呈现窗口到 DirectX 首次 `Clear(Black)` 之间存在时间差，Windows GDI 会绘制 WinForms 默认浅色背景；
- **方案**：通过 Prepatcher 直接在 `Terraria.Main..ctor` 退出前织入 IL，在窗口首次 Show 之前直接将 Form 背景设为纯黑、替换窗口类画刷为 `BLACK_BRUSH` 并拦截 `WM_ERASEBKGND`，实现 0 毫秒暗黑启动；同时移除高频拦截，根除日志刷屏。

### 4.2 高刷新率硬件光标即时采样
- **根因**：原版将鼠标采样锁死在 60Hz `Update` 逻辑帧，在高刷屏（144Hz+）上产生卡顿与输入延迟；
- **方案**：在渲染帧光标图层前直接调用 Win32 API（`GetCursorPos` + `ScreenToClient`）执行微秒级即时采样，消除操作粘滞感。

### 4.3 鼠标滚轮消息链与焦点隔离治理
- **根因**：WinForms 子类化破坏了 XNA `MouseMessageHooker` 消息链，且悬停检测边界缺失导致快捷栏与左侧快速制造栏误滚动；
- **方案**：修复 XNA 滚轮同步，并在 `Patch_HotbarScroll` 中新增 `DoScrollingInInventory` 拦截与 16px 窗口悬停容差判断，确保光标在模组窗口内时完全隔离原版快速制造栏与快捷栏切换。

### 4.4 挖掘范围指示框实时渲染重构
- **根因**：`DrawAreaPreview` 中的 `finally { _drawPos = null; }` 单次消费机制与 Update/Draw 解耦时钟冲突，在高刷屏下引发高频交替跳闪；
- **方案**：废除中间变量，改由每帧直接根据 `Main.LocalPlayer.HeldItem` 实时手持驱动渲染，并将指示框高度与实际开凿目标深度 100% 精确对齐。

### 4.5 随身便携制作站状态常驻缓存重构（根治合成列表闪烁）
- **根因**：原版 `Player.AdjTiles` 每帧执行 `Array.Clear(adjTile)` 将玩家制作站全部清零，而随身制作站原有 15 tick 节流导致在跳过的 14 帧内随身工作台全部丢失，引发制作列表每 0.25 秒剧烈震荡刷新并疯狂闪烁；
- **方案**：重构为“15 tick 周期性全量扫描更新静态缓存 + 逐帧快速合并”架构，玩家随身制作站状态每帧绝对稳定常驻，彻底根除 Crafting UI 闪烁。

### 4.6 天顶世界/颠倒世界图格安全接管与空指针防御
- **根因**：天顶世界（颠倒世界/地狱深处）玩家周围网格存在未实例化的 `null` 空图格对象（`Main.tile[i, j] == null`），原版 `Player.AdjTiles` 未判空直接调用 `tile.active()` 导致 NRE 异常抛出，中断主更新循环，表现为打开背包无法移动、吸钱币停滞、开闭设置崩溃；
- **方案**：在 `Patch_Player.cs` 注入框架级 `AdjTilesPrefix` 安全接管并集成全量诊断日志探针，提供 `PlayerAdjTileExtensions` 安全扩展方法，彻底杜绝 NRE 崩溃中断。

---

## 5. 自动化测试与 GABP 生态规范

为确保模组功能与游戏主循环的高质量交付，仓库建立了基于 **GABP (Game Agent Bridge Protocol)** 的端到端自动化测试体系：
1. **宿主注入端 (`TPMLBridge`)**：作为原生模组监听 GABP 协议指令，在游戏主线程队列（`MainThreadQueue`）中安全调度游戏状态；
2. **自动化测试套件 (`Scripts/test_tpml_*.py`)**：基于 Python 编写的端到端自动化测试脚本，全面覆盖物品注册、贴图尺寸、配方注入、向导查询、快捷栏规整、手持绘制防崩、实机使用开凿与矿道物理切片质量扫描；
3. **双模态存档只读保护机制**：
   - **日常游玩模式（默认）**：`WorldSaveProtectionEnabled = false`，玩家正常游戏与读写存档 100% 原版执行；
   - **自动化测试会话（自动接管）**：外部测试调度接管时动态激活保护，通过 Harmony 补丁拦截所有 `WorldFile.SaveWorld` 磁盘写操作，退出测试后自动重置，严禁污染或破坏玩家真实世界存档。

---

## 6. 全域模组持久化 (Sidecar) 与生命周期隔离管理

- **伴随存档存储 (`SidecarSaveManager`)**：将模组物品与扩展容器持久化数据存储于 `TPML_Saves/Player_<PlayerName>.tpml_data` 与 `TPML_Saves/World_<WorldName>_<WorldID>.tpml_data`；
- **全生命周期级联删除**：在 `Patch_Main` 中挂钩原版 `Main.ErasePlayer` 与 `Main.EraseWorld`，当玩家在游戏主菜单删除角色或世界存档时，自动级联清理 `TPML_Saves` 目录下的伴随存档文件（含 ID 兜底匹配），防止无效数据残留；
- **扩展容器隔离与自动清理 (`ResetContainers`)**：
  - **活动角色身份严格校验**：`BigBag` 与 `AccessoryBox` 维护当前在内存中持有槽位的 `ActivePlayerName`；在原版保存角色（`SavePlayerPrefix`）时，严格校验被保存角色是否与当前激活角色一致，杜绝新建角色首次保存时将其他角色的静态内存残留数据写入新角色伴随存档；
  - **退出世界与切换角色自动复位**：离开世界退回主菜单（`gameMenu` 状态切换）或激活新角色（`SetAsActive`）时，自动触发 `ModItemSidecarEngine.ResetContainers()` 广播，将大背包、饰品箱内存槽位重设为空白数组，并复位吸管工具（`PipetteEngine`）调度状态，保证开局纯净无污染。

---

## 7. 大背包 (BigBag) 快捷键与物品栏交互优化

- **关闭大背包独立解耦**：在 [`ModifyInterfaceLayers.cs`](file:///c:/Users/loris/Documents/Cirno9TerrariaMods/tPlainModLoader/Mods/OptimizeAndTool/OptimizeAndTool/OptimizeAndTool/ModifyInterfaceLayers.cs) 中优化了 `SwitchBigBag` 方法，按快捷键关闭大背包时仅关闭自身窗口并播放 `SoundID.MenuClose` 音效，不再强制将 `Main.playerInventory` 设为 `false`，原版物品栏保持开启状态；
- **打开大背包同步开启物品栏**：保持原有机制，若当前物品栏未开启，打开大背包时自动同步开启物品栏；
- **关闭物品栏同步关闭大背包**：保持原有机制，当玩家关闭原版物品栏（`!Main.playerInventory`）时，大背包窗口自动同步关闭。

---

## 8. 城镇 NPC 回房机制全面重构与换房即时瞬移

- **房间空间隔离算法 (`TeleportNPCToChairOrHome`)**：改用 `WorldGen.StartRoomCheck` 与 `WorldGen.Housing_CheckIfInRoom` 精准获取目标房屋的几何图格泛洪边界，坐具（椅子/床/沙发/马桶）检测严格限制在当前房间内部，彻底消除多层紧凑公寓中上下层或隔壁房间坐具串联、互相占用的问题；
- **插旗换房即时瞬移 (`Patch_WorldGen_MoveRoom`)**：在 Harmony 中为 `WorldGen.moveRoom` 挂载 Postfix，开启 `EnableInstantHousingTeleport` 时，玩家在房屋管理界面给 NPC 重新分配房间后，NPC 会伴随传送音效与粒子立即瞬移至新房间坐具上入座；
- **主循环精简与一键召回**：移除了主循环中的夜间每秒强制坐姿轮询，白天 NPC 保持正常自由活动；右键房屋管理图标或执行 `/townNPCHome` 指令可一键精准召回所有存活 NPC 与宠物；
- **设置面板与持久化**：在 `SettingUI` 中同步新增 `NPCInstantHousingTeleport` 与 `NPCNightAutoHome` 配置项的保存/读取/重置链路。

---

## 9. 统一 TPML.Content 生命周期与卸载闭环（2026-08-27）

- 新增 `ContentHost`，统一管理 `TPML.Content.Mod` 的注册、`Load`、配方构建与卸载清理；
- `tContentPatch.LoadInstance` 在加载程序集后自动 `ContentHost.RegisterFromAssembly`，内容模组不再需要各自手工调用 `ContentHookDispatcher.Initialize/RegisterMod/Load`；
- `tContentPatch.ModLoader` 在所有模组 Load/Loaded 完成后统一调用 `ContentHost.CompleteLoading()`，配方只需构建一次；
- 模组卸载统一走 `ContentHost.UnloadAll()`：逆序调用内容模组 `Unload`，并清除 Hook、RecipeLoader、ModPlayer 实例、ItemLoader 与 ModContent；
- 迁移入口：Instavator、FishingMachine、RecipeBrowser、OptimizeAndTool 改为从 `ContentHost.Find<T>()` 获取内容模组实例，保留各自的旧引擎 Harmony/Patch 钩子职责；
- 全量解决方案 Release 构建通过，0 错误。

---

## 10. 低风险工具下沉 TPML.Core（2026-08-27）

- 将无游戏依赖的 `Json 文件读写`、`字段浅复制`、`配置加载/保存` 从 `tContentPatch.Utils` 下沉到 `TPML.Core`；
- 新实现：`TPML.Core.Json.JsonHelper`、`TPML.Core.Reflection.ObjectCopy`、`TPML.Core.Configuration.ConfigStore<T>`；
- `tContentPatch.Utils.MyJson1 / CopyClass / ConfigHelp<T>` 保留为旧命名空间转发门面，旧模组无需改动即可继续编译；
- `ModFile`、`Resource`、`GameWindowDarkener`、`TCPC/TCPS` 等仍需旧宿主协作的类暂不迁移；
- 全量解决方案 Release 构建通过，0 警告 0 错误。

---

## 11. Windows IME 故障排查与根因收敛（2026-08-28）

- **线程模型与已排除方向**：
  - `LaunchGame` 已改为专用 STA 游戏线程，日志确认线程为 `STA`；
  - 确认 `DrawIME` 补丁不参与消息接收与初始化，原版 `DoDraw` 自行绘制候选面板；
  - 确认 TPML 与 Terraria 的 `ReLogic.dll` / `ReLogic.Native.dll` 二进制一致；
- **全链路诊断定位**：
  - 通过临时 `ImeDiagnostics` 拦截与真实键盘复现，捕获到关键特征：输入框聚焦时 `PlayerInput.WritingText=true`，`WindowsIme.Enable()` 被调用且托管 `IsEnabled=true`，但原生 `ImeUi_IsEnabled=false`，窗口未绑定 HIMC，仅产生 ASCII `WM_CHAR`；
  - 反汇编 `ReLogic.Native.dll` 确认：`ImeUi_Initialize(HWND)` 在启动时调用 `ImmGetContext(hwnd)`，若窗口无 HIMC 则返回 0，原生库会设置永久禁用标记，导致后续 `Enable(true)` 均被直接忽略；
- **生命周期机制厘清**：
  - `ImeContextBootstrap` 在初始化前补上 HIMC 后，ReLogic 成功初始化并在空闲时主动执行 `ImeUi_EnableIme(false)` 解除窗口关联（`ImmAssociateContext(hwnd, NULL)`），此为原版设计；
  - 激活输入框时，原版 `PlatformIme.Enable()` 会调用 `ImeUi_EnableIme(true)` 重新挂回最初保存的 HIMC，生命周期本身即为完备自洽闭环。

---

## 12. Windows IME 极简 Bootstrap 架构与零 Hook 交付（2026-08-28）

- **核心修复落地**：
  - 由 Prepatcher 在 `Main.ClientInitialize()` 的 `Platform.InitializeClientServices(HWND)` 前织入 `ImeContextBootstrap.EnsureAssociated(HWND)`；
  - 优先通过 `ImmAssociateContextEx(..., IACE_DEFAULT)` 恢复系统默认上下文，失败时创建并绑定新上下文，保证 `ImeUi_Initialize` 顺利完成；
- **清理多余代码与零 Hook 交付**：
  - 删除了全部临时诊断代码（`ImeDiagnostics.cs`，570+ 行）及多余的运行时 Harmony Patch；
  - `ImeContextBootstrap.cs` 保持为纯静态 Win32 API 辅助类（仅 89 行），无任何运行时 Hook 拦截，零侵入、零开销；
- **验证结论**：
  - 全量解决方案 20 个项目 Release 构建通过，0 警告 0 错误，自动热部署完毕；
  - 实机真实键盘在角色名、世界名和游戏内聊天框中连续输入中文、拼音合成与候选选词均 100% 正常。



