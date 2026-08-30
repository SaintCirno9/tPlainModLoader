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

---

## 13. M2 引擎全量 MonoMod 迁移与 Item.SetDefaults 原生 IL 织入根治（2026-08-30）

### 13.1 引擎全量 MonoMod 迁移落地
- **依赖统一**：统一全仓 `Mono.Cecil` 为 0.11.4 版本；`TPML.Content` 与 `tContentPatch` 彻底移除 `Lib.Harmony` 引用，改用 `MonoMod.RuntimeDetour 22.7.31.1`；
- **集中管理**：引入 `HookRegistry`，集中管理 MonoMod On 风格 Detour 生命周期与统一反注册；
- **补丁集中重构**：
  - `ContentHookDispatcher`（16 个动态派发点）与 `Patch_UnifiedInventoryFusion`（19 个背包融合拦截点）全量转换为强类型显式 MonoMod `Hook`；
  - `tContentPatch` 25 个静态属性 Patch 类全量改写为显式注册；
  - 仓库模组采用 tML 标准生态的 `On.` 门面（`MMHOOK_Terraria.dll`）机制。

### 13.2 Item.SetDefaults 原生 IL 短路根治
- **根本原因定位**：原版 `Terraria.Item.SetDefaults` 内部在 `[001E]` 处硬编码了 `if (this.type >= ItemID.Count) this.type = 0;`。过去依赖的运行时 Detour 易受 JIT 内联展开与下游模组 Harmony Patch 覆盖而丢失，导致模组物品 ID 穿透至原版后被强制置 0 变空气；
- **底层架构根治**：
  - 在 `ItemLoader` 中增加静态纯纯拦截入口 `OnSetDefaultsPrefix(Item, int)`；
  - 在 `PrepatcherEngine` 启动引导期，使用纯静态 Cecil 元数据向 `Item.SetDefaults(int, ItemVariant)` 与 `Item.netDefaults(int)` 头部织入原生 IL 短路指令（`ldarg.0; ldarg.1; call OnSetDefaultsPrefix; brfalse.s ...; ret;`）；
  - 模组物品在进入原版逻辑前即由 `ItemLoader` 完成实体初始化并直接 `ret`，原版清零代码永远不会被执行；原版物品代码 100% 毫无改变地原样执行；
- **清理冗余代码**：彻底移除了所有调用方（`GiveItem`、`SidecarTools`、`AccessoryBagTools`、`RecipeLoader`、`ModItemSidecarEngine` 等）中所有的临时 `if (item.type != type || item.IsAir)` 兜底代码，恢复纯净原生调用。

### 13.3 实机自动化测试全量回归验收
- **全量构建**：19 个工程 `dotnet build ...sln -c Release -m /graph` 0 警告 0 错误（~5.5s）；
- **GABS 实机测试套件验收**：
  - `test_tpml_sidecar_persistence.py` **10/10 PASS**
  - `test_tpml_sidecar_containers.py` **9/9 PASS**
  - `test_tpml_instavator.py` **10/10 PASS**
  - `test_tpml_accessory_bag.py` **11/11 PASS**
  - `test_tpml_creative_inventory.py` **PASS**
  - `test_tpml_recipe_browser.py` **PASS**
  - `test_tpml_inventory_fusion.py` **12/12 PASS**
  - `test_tpml_item_containers.py` **10/10 PASS**
  - `test_tpml_scroll_wheel.py` **PASS**
- **出口判定**：M2 引擎 MonoMod 迁移与核心回归全量达标，已正式标记为完成。

---

## 14. M3 阶段：TPML.Content tML API 兼容层与资产/本地化引擎补齐（2026-08-30）

### 14.1 核心 tML 兼容性增强（B1–B6）
- **MonoMod 门面（工作包 A）**：
  - 新增 `Terraria.ModLoader.MonoModHooks`（支持 `Add` / `Modify` / `RequestNativeAccess`），直连 MonoMod RuntimeDetour 与 HookGen，对齐 tML 门面。
- **生命周期扩展（B1 & B2）**：
  - `ModType`、`ModPlayer`、`ModSystem` 扩充无参 `public virtual void Load()` 与 `public virtual void PostSetupContent()`；
  - `ModType.Load(Mod)` 自动派发 `this.Load()`，完全兼容 tML 风格子类生命周期；
  - 在 `LoaderControl.OnModLoad_Ok` 触发阶段全域派发 `ModContent.PostSetupContent()`。
- **Mod 查找与状态（B3）**：
  - `ModContent` 与 `ModLoader` 扩充 `TryGetMod(string name, out Mod mod)` 与 `GetMod(string name)`。
- **资产管线（B4）**：
  - `ModAssetRepository` 与 `ModContent.Request<T>` 真实实现：支持从嵌入资源和模组文件系统中解析 `.png` 与 `.rawimg` 并封装为 `Asset<Texture2D>` 缓存，缺失时安全返回 `Asset<T>.Empty` 并记录日志。
- **TagCompound 与 Item 序列化层（B5）**：
  - 在 `TagCompound` 与 `ItemIO` 中实现原版与模组物品无损序列化/反序列化（`ItemIO.Save(item)` / `ItemIO.Load(tag)`）；
  - `tag.Get<Item>("key")` 能够自动从 JObject 或嵌套 `TagCompound` 中无缝还原 `Item` 实例。
- **Player.KillMe 派发（B6）**：
  - `ContentHookDispatcher` 挂钩 `Player.KillMe`，先派发 `ModPlayer.PreKill`（返回 false 则取消死亡），再执行原版死亡逻辑，后派发 `ModPlayer.Kill`。

### 14.2 引擎级 Hjson 本地化自动加载器（工作包 C）
- **`LocalizationLoader`**：
  - 自动扫描模组嵌入资源中的 `*.hjson`，支持多层级嵌套、注释与三引号多行字符串；
  - 自动将翻译词条安全注入到原版 `LanguageManager._localizedTexts` 与 `_categoryGroupedTranslations` 中。

### 14.3 构建与全量实机回归
- **构建结果**：19 个工程 `dotnet build` 0 警告 0 错误（~7.4s 极速构建）；
- **实机测试套件**：9 大自动化测试套件（Sidecar、容器、直通车、饰品袋、创造浏览器、合成表、背包融合、药水袋/旗帜盒、滚轮快捷栏）全量回归 **100% PASS**。

---

## 15. M4 阶段：PotionSlots 模组完整移植与自动化实机回归（2026-08-30）

### 15.1 移植与工程架构
- **新建工程与依赖配置**：
  - 新建 `tPlainModLoader/tPlainModLoader/TerrariaHooks/TerrariaHooks.csproj`（提供 MonoMod HookGen `On_Player.QuickHeal_GetItemToUse` 与 `QuickMana_GetItemToUse` 门面）；
  - 新建 `tPlainModLoader/Mods/PotionSlots/PotionSlots/PotionSlots.csproj`（引用 `TerrariaHooks`、`tContentPatch`、`TPML.Content`，不依赖 `Lib.Harmony`）；
  - 自动部署：配置 `[DeployToGameDir]` Target，Release 构建时自动将二进制与元数据发布至 `tPlainModLoader\Mods\PotionSlots\`。
- **引擎自动发现与生命周期激活**：
  - 在 `ContentHost.Register` 中增强了对程序集内部所有非抽象 `ILoadable`（`ModPlayer`、`ModSystem`、`ModItem`）的自动反射发现与 `mod.AddContent` 注册；
  - 使 `PotionStoragePlayer.Load()` 与 `UILoader.Load()` 能够在模组启动期被自动激活，完成 MonoMod 事件订阅与 UI 图层注册。

### 15.2 源码清理与兼容适配对比
- **强类型零反射对齐**：通过 `Krafs.Publicizer` 与 `TerrariaHooks`，所有原版方法与钩子均采用强类型调用；
- **鼠标事件裁剪**：裁剪了原版 `UIElement` 中不存在的 `XButton1/2` 与 `MiddleClick` 虚方法重写，保持原版 XNA/UI 管道稳定；
- **死亡掉落物品签名**：使用原版 `Item.NewItem(Player.GetSource_Misc("PlayerDeath"), Player.position, item.type, item.stack)` 替代重载不匹配的临时参数。

### 15.3 自动化实机测试与回归结果
- **全量构建验证**：全量 21 个工程构建耗时 4.22s，0 警告 0 错误；
- **PotionSlots 专用回归测试套件 (`tpml/test_potion_slots`)**：**5/5 项全部 PASS**
  1. `1. 槽位赋值`：成功初始化 `lifeSlot(10)`、`manaSlot(15)`、`wormholeSlot(5)`；
  2. `2. QuickHeal 钩子消耗与治疗`：血量 20 -> 70，槽位药水由 10 消耗至 9；
  3. `3. QuickMana 钩子消耗与回蓝`：魔力 0 -> 20，槽位魔力药水由 15 消耗至 14；
  4. `4. OnPickup 自动拾取合并`：拾取药水后槽位自动补齐至 12，拾取物品归 0；
  5. `5. TagCompound 存档序列化/反序列化`：槽位数据在内存与 TagCompound 转换中 100% 保真恢复；
- **历史套件全量回归**：全域 10 大测试套件（Sidecar 存取、Instavator、AccessoryBag、Creative 背包、RecipeBrowser、背包融合、ItemContainers、滚轮模拟）**100% 全部通过**。

---

## 16. M5 阶段：TPML 全量 HookGen 引擎与 Detour 自动追踪回收架构落地（2026-08-30）

### 16.1 全量自动化 HookGen 生成器 (`TPML.HookGen`)
- **独立工具工程**：新建 `TPML.HookGen` 控制台工具工程，基于 Cecil 静态扫描原版 `Terraria.exe` 命名空间下的所有类与方法；
- **强类型事件与委托生成**：自动为每个原版类生成 `orig_` 委托、`hook_` 委托（带 `orig` 参数）、`On.<Namespace>.<Class>.<Method>` 事件（IL 织入 `ldtoken` + `MethodBase.GetMethodFromHandle` + `HookEndpointManager.Add/Remove`）与 `IL.<Namespace>.<Class>.<Method>` 事件（`HookEndpointManager.Modify/Unmodify`）；
- **对齐 tML 标准命名**：生成类型归属于 `On.<Namespace>` 与 `IL.<Namespace>`（如 `On.Terraria.Player`、`IL.Terraria.Player`），1 秒内全量生成 1305 个类型与 15605 个方法钩子，完全对齐 tModLoader 标准生态。

### 16.2 Detour 运行时生命周期自动追踪与零泄漏回滚 (`MonoModHooks`)
- **程序集级生命周期维护**：在 `TPML.Content.Engine.MonoModHooks` 中按调用方/定义方 Mod 程序集自动记录持有的 Detour 与 ILHook 列表；
- **无感自动卸载回滚**：在 `ContentHost.UnloadAll()` 与 `ModLoader.Unload()` 阶段，自动遍历释放所有已注册的 Hook 并重置 `HookEndpointManager` 字典与反射缓存，彻底消除模组未手动 `-=` 解绑时的内存残留与幽灵钩子；
- **tML 兼容门面**：提供与 tModLoader 完全对齐的 `MonoModHooks.Add` / `Modify` / `DumpOnHooks` / `DumpILHooks` / `DumpIL` 门面。

### 16.3 彻底清理历史遗留项
- **废弃 HookBinder**：彻底删除旧 `IAddPatch` 动态编译生成器 `HookBinder.cs`，清理 `PatchUtil.cs`；
- **升级 Mono.Cecil**：全仓统一升级 `Mono.Cecil` 至 0.11.5 版本，对齐最新工具链；
- **移除旧版 MMHOOK 声明**：从 `Directory.Build.props` 中移除对外部旧 `MMHOOK_Terraria.dll` 的静态引用，通过 `Directory.Build.props` 全局自动引用新架构生成的 `TerrariaHooks.dll`；
- **全量构建验证**：全量 24 个工程 `dotnet build ...sln -c Release -m -graph` 0 警告 0 错误编译通过并完成自动热部署（~4.9s）。






