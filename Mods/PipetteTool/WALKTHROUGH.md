# 吸管工具 (PipetteTool) 架构设计与实施验收

> **模组名称**：吸管工具 (PipetteTool / Pick Block)  
> **适用版本**：Terraria 1.4.5.7 / tPlainModLoader (`net472`)  
> **作者**：SaintCirno9  

---

## 1. 功能概述与设计定位

本模组为 `tPlainModLoader` 提供了类似 **Factorio 吸管工具 (Pipette Tool / Pick Block)** 的纯指针调度建造体验：
- 鼠标指向世界中的物块、复杂多格家具（工作台、火把、床、门、平台等）或背景墙；
- 按下快捷键（默认 `Q` 键，可在游戏内设置自由重绑）；
- **纯指针手持选中（零物品栏破坏）**：
  - 无论目标物品位于快捷栏（0~9）还是主背包（10~49），模组直接通过 `player.selectedItemState.Select(slot)` 将手持槽位临时指向该物品，**不移动、不对调、不破坏背包中的任何物品排布与快捷栏**；
- **吸管前原始快捷栏记忆与状态机恢复**：
  - 初次吸管成功时锁定 `lastNormalSlot`，并记录 `currentPipetteSlot`；
  - 若鼠标指向**空气/无效区域**按 Q，或者手持该物块时**指向相同物块再次按 Q**，无条件精准恢复回 `lastNormalSlot`；
- **未持有提示**：
  - 若背包中不存在该物块，在玩家头顶弹出橙红色提示 `背包中未找到: [物块名]` 并播放提示音。

---

## 2. 核心架构与模块划分

```
PipetteTool/
├── PipetteTool.csproj                  # SDK 风格 MSBuild 工程文件（含 DeployToGameDir）
├── PipetteTool.sln                     # 独立解决方案文件
├── PipetteToolMod.cs                   # 模组主入口（继承 PatchMain）
├── info.json                           # 模组元数据声明
├── loadConfig.json                     # 默认加载配置
├── Properties/
│   └── AssemblyInfo.cs                 # 程序集元数据
├── Core/
│   ├── TileToItemResolver.cs           # 图格与背景墙到物品 ID 的双向反向智能映射器
│   └── PipetteEngine.cs                # 吸管触发、状态机记忆与纯指针选中调度核心引擎
├── Config/
│   ├── PipetteConfig.cs                # 全局运行时配置与 JSON 序列化数据结构
│   └── PipetteSetting.cs               # 继承 ModSetting，提供 UIKeyBindItem 与设置控件
├── Input/
│   └── PipetteKeyHandler.cs            # 边缘触发键盘/鼠标按键捕获与打字防冲突
└── ModLinkage/
    ├── ModQuickSetting.cs              # 联动 QuickSetting 抽屉菜单
    └── LinkageSetting.cs               # 模组联动选项管理
```

---

## 3. 验证与构建测试

1. **单 Mod 编译验证**：
   ```pwsh
   dotnet build tPlainModLoader/Mods/PipetteTool/PipetteTool.sln -c Release
   ```
   - 耗时 ~1.9s，0 错误，0 警告；
   - 自动生成 `PipetteTool.dll`、`PipetteTool.pdb`、`info.json`、`loadConfig.json` 并增量部署至 `$(TerrariaDir)\tPlainModLoader\Mods\PipetteTool\`。

2. **tPlainModLoader 全量解决方案构建验证**：
   ```pwsh
   dotnet build tPlainModLoader/tPlainModLoader/tPlainModLoader.sln -c Release -m /graph
   ```
   - 20 个项目全量构建通过，耗时 ~4.4s，无任何回归问题。
