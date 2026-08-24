# tPlainModLoader 统一快捷键系统接入 Walkthrough

## 概述

已完成将 `tPlainModLoader` 仓库下所有子 Mod 当前自制的快捷键系统全面迁移并接入 Loader 核心的统一快捷键框架（`KeybindLoader` / `ModKeybind` / `Patch_UIManageControls`）。

---

## 实施详情与变更对照

### 1. 统一快捷键注册与对应关系

| 模组名称 | 快捷键标识符 | 默认按键 | 显示名称 | 触发效果 |
| :--- | :--- | :--- | :--- | :--- |
| **CreativeInventory** | `ToggleCreativeInventory` | `C` | `开关创造模式物品栏 (Toggle Creative Inventory)` | 打开/关闭创造模式物品浏览器 UI |
| **QuickSetting** | `ToggleQuickSetting` | `F` | `开关快捷设置菜单 (Toggle Quick Setting)` | 打开/关闭快捷设置侧边栏菜单 UI |
| **WandsTool** | `ToggleWand` | `Z` | `开关魔杖模式 (Toggle Wand Mode)` | 切换魔杖模式开启/关闭 |
| **WandsTool** | `FlipHorizontal` | `H` | `魔杖蓝图: 水平镜像翻转 (Flip Horizontal)` | 蓝图粘贴模式下水平翻转剪贴板 |
| **WandsTool** | `FlipVertical` | `V` | `魔杖蓝图: 垂直镜像翻转 (Flip Vertical)` | 蓝图粘贴模式下垂直翻转剪贴板 |
| **OptimizeAndTool** | `ToggleBigBag` | `X` | `开关巨大额外背包 (Toggle Big Bag)` | 打开/关闭额外大背包界面 |
| **PipetteTool** | `PickColor` | `Q` | `吸取物块与颜色样式 (Pick Block)` | 吸取光标处方块/墙壁并切换快捷栏手持 |

---

### 2. 子 Mod 核心改造点

#### 1) `CreativeInventory`
- 新增 [`CreativeInventoryKeybind.cs`](file:///c:/Users/loris/Documents/Cirno9TerrariaMods/tPlainModLoader/Mods/CreativeInventory/CreativeInventory/CreativeInventory/KeyBind/CreativeInventoryKeybind.cs)，向 `KeybindLoader` 注册 `ToggleCreativeInventory`（默认 `C`）；
- 改造 [`ModifyInterfaceLayers.cs`](file:///c:/Users/loris/Documents/Cirno9TerrariaMods/tPlainModLoader/Mods/CreativeInventory/CreativeInventory/CreativeInventory/ModifyInterfaceLayers.cs)，在 `UpdateUIStatesPostfix` 中通过 `CreativeInventoryKeybind.ToggleKeybind.JustPressed` 控制开关；
- 改造 [`SettingKeyBind.cs`](file:///c:/Users/loris/Documents/Cirno9TerrariaMods/tPlainModLoader/Mods/CreativeInventory/CreativeInventory/CreativeInventory/KeyBind/SettingKeyBind.cs) 与 [`UIKeyBind.cs`](file:///c:/Users/loris/Documents/Cirno9TerrariaMods/tPlainModLoader/Mods/CreativeInventory/CreativeInventory/CreativeInventory/KeyBind/UIKeyBind.cs)，只读展示当前绑定的按键并引导玩家前往原版控件设置修改；
- 彻底移除老旧的 `ListenInput.cs`。

#### 2) `QuickSetting`
- 新增 [`QuickSettingKeybind.cs`](file:///c:/Users/loris/Documents/Cirno9TerrariaMods/tPlainModLoader/Mods/QuickSetting/QuickSetting/QuickSetting/KeyBind/QuickSettingKeybind.cs)，向 `KeybindLoader` 注册 `ToggleQuickSetting`（默认 `F`）；
- 改造 [`ModifyInterfaceLayers.cs`](file:///c:/Users/loris/Documents/Cirno9TerrariaMods/tPlainModLoader/Mods/QuickSetting/QuickSetting/QuickSetting/ModifyInterfaceLayers.cs)，在 `UpdateUIStatesPostfix` 中通过 `QuickSettingKeybind.ToggleKeybind.JustPressed` 控制开关；
- 改造 [`SettingKeyBind.cs`](file:///c:/Users/loris/Documents/Cirno9TerrariaMods/tPlainModLoader/Mods/QuickSetting/QuickSetting/QuickSetting/KeyBind/SettingKeyBind.cs) 与 [`UIKeyBind.cs`](file:///c:/Users/loris/Documents/Cirno9TerrariaMods/tPlainModLoader/Mods/QuickSetting/QuickSetting/QuickSetting/KeyBind/UIKeyBind.cs)；
- 彻底移除老旧的 `ListenInput.cs`。

#### 3) `WandsTool`
- 新增 [`WandsKeybind.cs`](file:///c:/Users/loris/Documents/Cirno9TerrariaMods/tPlainModLoader/Mods/WandsTool/WandsTool/WandsTool/KeyBind/WandsKeybind.cs)，集中注册 `ToggleWand`（`Z`）、`FlipHorizontal`（`H`）、`FlipVertical`（`V`）；
- 改造 [`feces.cs`](file:///c:/Users/loris/Documents/Cirno9TerrariaMods/tPlainModLoader/Mods/WandsTool/WandsTool/WandsTool/feces.cs)，在 `DoUpdateInWorldPostfix` 中通过 `WandsKeybind.ToggleWand.JustPressed` 切换魔杖模式；
- 改造 [`Wands.cs`](file:///c:/Users/loris/Documents/Cirno9TerrariaMods/tPlainModLoader/Mods/WandsTool/WandsTool/WandsTool/Content/Wands/Wands.cs)，替换硬编码的 `Keys.H` / `Keys.V` 为 `FlipHorizontal.JustPressed` 与 `FlipVertical.JustPressed`；
- 改造 [`SettingKeyBind.cs`](file:///c:/Users/loris/Documents/Cirno9TerrariaMods/tPlainModLoader/Mods/WandsTool/WandsTool/WandsTool/KeyBind/SettingKeyBind.cs) 与 [`UIKeyBind.cs`](file:///c:/Users/loris/Documents/Cirno9TerrariaMods/tPlainModLoader/Mods/WandsTool/WandsTool/WandsTool/KeyBind/UIKeyBind.cs)；
- 彻底移除老旧的 `ListenInput.cs`。

---

## 构建与验证结果

- **全量解决方案构建**：
  ```pwsh
  dotnet build tPlainModLoader/tPlainModLoader/tPlainModLoader.sln -c Release -m /graph
  ```
  - **结果**：18+ 个工程全量构建通过，**0 个警告，0 个错误**，所有产物及元数据已自动打包部署至游戏目录。
- **无冗余代码检查**：全仓库不再存在任何 `ListenInput` 监听器或直接按键轮询，所有模组按键均统一由原版 `PlayerInput` 驱动。

---

## 2026-08-25: 鼠标滚轮修复、创造物品浏览器搜索重构与自动化测试存档双模态保护

1. **鼠标滚轮修复**：
   - 修复 `Patch_HotbarScroll.cs` 误将悬停在快捷栏或 HUD 上的滚轮事件与数字键拦截的问题，限制为仅在活动模组窗口内悬停才屏蔽快捷栏滚轮；
   - 优化 `Patch_Main.cs` 前置捕获并安全分发 UI 滚轮事件至模组 `UserInterface`。
2. **创造模式物品浏览器搜索重构**：
   - 重构 `UITextBox.cs`：获得 Focus 时锁定 `PlayerInput.WritingText` 与 `Main.CurrentInputTextTakerOverride`，挂载 IME 锚点，支持 Enter/Esc 提交，搜索字符上限放宽至 50；
   - 优化 `UICreativeInventory.cs`：将搜索行容器转为 `UIPanel` 并接入自适应拉伸；修正单选框初始化时序（解决构造 `NullReferenceException` 导致窗口无法打开问题）；支持大小写不敏感模糊匹配与数字 ItemID 检索。
3. **TPMLBridge 扩展与双模态存档保护**：
   - 默认日常游玩模式（`WorldSaveProtectionEnabled = false`）：100% 原版正常游玩与写盘保存；
   - 自动化测试模式（`tpml/load_world` / GABP 会话）：动态激活保护，拦截 `WorldFile.SaveWorld`，退出测试（`tpml/leave_world`）后自动复位。
