# 模组快捷键系统全面接入 tPlainModLoader 统一按键框架 实施计划

## 概述与背景

目前 `tPlainModLoader` 核心已实现基于原版 `Terraria.GameInput.PlayerInput` 与 `UIManageControls` 的原生级快捷键框架（`KeybindLoader` / `ModKeybind` / `Patch_UIManageControls`）。
该框架具备以下核心能力：
1. **原生设置界面无缝集成**：在原版 ESC/主菜单的【控件 (Controls)】页面自动注入【模组按键 (Mod Controls)】分组，支持查看与绑定键盘、手柄等任意输入模式；
2. **原生持久化**：统一保存于原版 `input profiles.json`，无需各个 Mod 单独创建并读写 `keyBind.json`；
3. **输入静默保护**：在玩家打字、聊天、编辑告示牌或箱子时自动全局静默，避免按键冲突；
4. **边缘触发与状态机**：提供 `Current`、`JustPressed`、`JustReleased` 等精准判定。

目前 `OptimizeAndTool`（巨大背包 `ToggleBigBag`）与 `PipetteTool`（吸管工具 `PickColor`）已接入该框架。
但 **`CreativeInventory`**、**`QuickSetting`** 与 **`WandsTool`** 仍在使用早期独立实现的 `ListenInput.cs`（每帧轮询 `PlayerInput.GetPressedKeys()` / `MouseKeys`）以及各自的 `keyBind.json`，并且 `WandsTool` 的蓝图翻转操作硬编码了 `Keys.H` 与 `Keys.V`。

本次重构目标：**将所有子 Mod 的快捷键系统全面接入 `tPlainModLoader` 统一快捷键框架，移除所有冗余的 `ListenInput.cs` 与老旧逻辑。**

---

## 拟接入与重构的子 Mod 清单

### 1. `CreativeInventory`（创造模式物品栏）
- **注册快捷键**：`ToggleCreativeInventory`
  - 默认按键：`C`
  - 显示名称：`开关创造模式物品栏 (Toggle Creative Inventory)`
- **主要改动**：
  - 新增 `CreativeInventoryKeybind.cs`，通过 `KeybindLoader.RegisterKeybind` 统一注册；
  - 改造 `ModifyInterfaceLayers.cs` 中的 `UpdateUIStatesPostfix`，监听 `CreativeInventoryKeybind.ToggleKeybind.JustPressed` 控制开关；
  - 改造 `SettingKeyBind.cs` 与 `UIKeyBind.cs` 为只读展示与原版控件菜单跳转提示控件；
  - 删除冗余的 `ListenInput.cs`。

### 2. `QuickSetting`（快捷设置）
- **注册快捷键**：`ToggleQuickSetting`
  - 默认按键：`F`
  - 显示名称：`开关快捷设置菜单 (Toggle Quick Setting)`
- **主要改动**：
  - 新增 `QuickSettingKeybind.cs`，通过 `KeybindLoader.RegisterKeybind` 统一注册；
  - 改造 `ModifyInterfaceLayers.cs` 中的 `UpdateUIStatesPostfix`，监听 `QuickSettingKeybind.ToggleKeybind.JustPressed` 控制开关；
  - 改造 `SettingKeyBind.cs` 与 `UIKeyBind.cs` 为只读展示与原版控件菜单跳转提示控件；
  - 删除冗余的 `ListenInput.cs`。

### 3. `WandsTool`（魔杖工具）
- **注册快捷键**：
  1. `ToggleWand`：默认按键 `Z`，显示名称 `开关魔杖模式 (Toggle Wand Mode)`
  2. `FlipHorizontal`：默认按键 `H`，显示名称 `魔杖蓝图: 水平镜像翻转 (Flip Horizontal)`
  3. `FlipVertical`：默认按键 `V`，显示名称 `魔杖蓝图: 垂直镜像翻转 (Flip Vertical)`
- **主要改动**：
  - 新增 `WandsKeybind.cs`，集中注册上述 3 个快捷键；
  - 改造 `feces.cs`，在游戏内生命周期中监听 `WandsKeybind.ToggleWand.JustPressed` 切换魔杖模式；
  - 改造 `Wands.cs` 中的 `Update()`，替换硬编码的 `Keys.H` / `Keys.V` 为 `FlipHorizontal.JustPressed` 与 `FlipVertical.JustPressed`；
  - 改造 `SettingKeyBind.cs` 与 `UIKeyBind.cs`；
  - 删除冗余的 `ListenInput.cs`。

---

## 涉及文件变动详细规划

```
tPlainModLoader/Mods/
├── CreativeInventory/
│   └── CreativeInventory/CreativeInventory/
│       ├── [NEW] KeyBind/CreativeInventoryKeybind.cs
│       ├── [MODIFY] ModifyInterfaceLayers.cs
│       ├── [MODIFY] CreativeInventory/CreativeInventory.cs
│       ├── [MODIFY] KeyBind/SettingKeyBind.cs
│       ├── [MODIFY] KeyBind/UIKeyBind.cs
│       └── [DELETE] ListenInput.cs
├── QuickSetting/
│   └── QuickSetting/QuickSetting/
│       ├── [NEW] KeyBind/QuickSettingKeybind.cs
│       ├── [MODIFY] ModifyInterfaceLayers.cs
│       ├── [MODIFY] QuickSetting/QuickSetting.cs
│       ├── [MODIFY] KeyBind/SettingKeyBind.cs
│       ├── [MODIFY] KeyBind/UIKeyBind.cs
│       └── [DELETE] ListenInput.cs
└── WandsTool/
    └── WandsTool/WandsTool/
        ├── [NEW] KeyBind/WandsKeybind.cs
        ├── [MODIFY] feces.cs
        ├── [MODIFY] Content/Wands/Wands.cs
        ├── [MODIFY] KeyBind/SettingKeyBind.cs
        ├── [MODIFY] KeyBind/UIKeyBind.cs
        └── [DELETE] ListenInput.cs
```

---

## 验证方案

1. **编译构建全量验证**：
   执行 `dotnet build tPlainModLoader/tPlainModLoader/tPlainModLoader.sln -c Release -m /graph`，确保 18+ 个项目全量构建 0 错误 0 警告，并自动部署到游戏目录。
2. **代码级语法与契约检查**：
   确保所有引用 `ListenInput` 的死代码彻底清除，强类型无反射，遵守 `Author: SaintCirno9` 规范。
