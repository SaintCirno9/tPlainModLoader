# OptimizeAndTool 模组全量重构与合并实施计划

> **目标**：将细碎的 6 个模组（`ReduceMouseLag`、`VeinMining`、`PipetteTool`、`SundryTool`、`AccessoryBox`、`CreativeInventory`）全量并入 `OptimizeAndTool`，抽离出可复用的公共基础设施（UI 控件库、按键注册、滚轮拦截、Shift 存取等），实现配置文件与玩家存档的向后兼容，并将工程总数由 20 个收敛至极简的 13 个。
> **维护者**：`SaintCirno9`

---

## 1. 架构调整与业务域划分

合并后的 `OptimizeAndTool` 将统一划分为 6 大核心业务域，结构清晰且高内聚：

```
Mods/OptimizeAndTool/
├── OptimizeAndTool.csproj          # 单一工程，统一输出并热部署
├── Command.cs                      # 统一主入口与 CLI 命令树
├── SettingUI.cs                    # 统一配置面板（整合所有子模块设置）
├── ModifyInterfaceLayers.cs        # 统一 UI 绘制层调度
├── ModLinkage/                     # 统一对接 QuickButton / QuickSetting 抽屉
│   ├── LinkageSetting.cs
│   ├── ModQuickButton.cs
│   └── ModQuickSetting.cs
├── Utils/                          # 🚀 抽离出的公共基础设施 (Shared Utils)
│   ├── BindUIAVal.cs               # 响应式数据绑定与变更事件
│   ├── CommandHRA.cs               # 命令辅助
│   ├── CommandVariable.cs          # 命令变量
│   ├── GetSetReset.cs              # 重置辅助
│   ├── UI/                         # 通用 UI 控件与构建器 (Shared UI)
│   │   ├── UIBuild.cs              # 统一树状/折叠/卡片 UI 渲染构建器
│   │   ├── UIItemSwitchBind.cs     # 开关控件
│   │   ├── UIItemTextBoxASwitchBind.cs # 文本输入+开关组合控件
│   │   ├── UIItemTextBoxBind.cs    # 文本输入控件
│   │   ├── UIItemButton.cs         # 按钮控件
│   │   ├── UITextBoxBind.cs        # 文本框控件
│   │   └── UIDraggableWindow.cs    # 通用窗口基类 (统管拖拽、边界防溢出与层级)
│   └── Patch/                      # 通用 Harmony 拦截器 (Shared Patches)
│       ├── HotbarScrollSuppressor.cs # 通用滚轮防切武器补丁 (BigBag/AccessoryBox/Creative)
│       └── ShiftTransferHelper.cs    # 通用 Shift 存取调度辅助
└── Content/
    ├── Optimize/                   # ⚡ 性能与底层优化
    │   ├── Lighting/ (原 Patch_Lighting)
    │   ├── ZoomLimit/ (原 PatchGameViewMatrixZoomLimit)
    │   └── ReduceMouseLag/ (原 ReduceMouseLag 硬件光标高频采样)
    ├── Storage/                    # 🎒 扩展容器系统
    │   ├── BigBag/                 # 500+ 格大背包系统与制作联动
    │   └── AccessoryBox/           # 40 格独立额外饰品盒与属性注入
    ├── QoL/                        # 🛠️ 体验与规则增强
    │   ├── VeinMining/             # 连锁挖矿 (BFS 矿石/宝石/化石递归采掘)
    │   ├── Pipette/                # 吸管工具 (Pick Block，物块/墙体快捷拿出与智能对调)
    │   ├── Potions/                # 无限药水 & Buff & 旗帜
    │   ├── Crafting/               # 便携制作站
    │   ├── Containers/             # 便携四箱
    │   ├── TownNPC/                # 城镇 NPC 幸福度 & 免房屋入住
    │   ├── Angler/                 # 渔夫无限任务
    │   ├── Stack/                  # 物品 9999 堆叠
    │   └── Chat/                   # 聊天防重 & 复制
    ├── Cheat/                      # 🎛️ 杂项辅助与调试 (原 SundryTool)
    │   ├── Player/                 # 上帝模式无敌、穿墙、倍速、飞行时间、呼吸
    │   ├── World/                  # 时间锁定、阻止腐化蔓延、生态检测、敌怪生成倍率
    │   ├── Visual/                 # 全图透视、全图照明、结构标记
    │   └── ItemModify/             # 手持物品与玩家属性实时微调
    ├── Creative/                   # 🎨 创造模式
    │   ├── Inventory/              # 物品浏览器窗口与网格
    │   └── ItemSort/               # 物品分类、ID 索引与全局检索
    └── ServerList/                 # 🌐 多人服务器列表与一键直连
```

---

## 2. 配置与玩家数据迁移策略

1. **统一配置 (`Config/OptimizeAndTool/setting.json`)**：
   - `SettingUI_player.Data` 中聚合所有子系统的设置字段。
   - **向后兼容加载**：若某模块在首次加载时未在主配置中找到对应键值，自动尝试检查并读取旧模组配置文件（如 `Config/VeinMining/config.json`、`Config/ReduceMouseLag/config.json`、`Config/SundryTool/setting.json`），平滑迁移后持久化至 `OptimizeAndTool/setting.json`。
2. **玩家存档数据向后兼容**：
   - 饰品盒数据存档继续保持在 `Save/AccessoryBox/<PlayerName>.json` 或提供统一读取 fallback，确保玩家已装备的 40 格饰品 100% 不丢失。
   - 大背包数据存档保持在 `Save/BigBag/<PlayerName>.json`。

---

## 3. 解决方案与项目文件调整

1. **从 `tPlainModLoader.sln` 移除 6 个被合并子工程**：
   - 移除 `AccessoryBox.csproj`
   - 移除 `CreativeInventory.csproj`
   - 移除 `PipetteTool.csproj`
   - 移除 `ReduceMouseLag.csproj`
   - 移除 `SundryTool.csproj`
   - 移除 `VeinMining.csproj`
2. **删除旧 Mod 目录**：
   - 安全迁移完成后，删除 `Mods/AccessoryBox/`、`Mods/CreativeInventory/`、`Mods/PipetteTool/`、`Mods/ReduceMouseLag/`、`Mods/SundryTool/`、`Mods/VeinMining/`，避免冗余和重复编译。
3. **保留并维护独立的专业模组**：
   - `QuickButton`（公共悬浮按钮栏总线）
   - `QuickSetting`（公共抽屉设置总线）
   - `OptimizeAndTool`（全能优化与工具箱）
   - `WandsTool`（独立建筑与蓝图套件）
   - `PixelArt`（独立像素画生成器）
   - `Skil`（独立魔法与战斗技能）
   - `SuspiciousPlayer`（独立史莱姆枪 BOSS 战）
   - `ChatAi`（独立 AI 对话助手）
   - `批量生成`（独立发布打包脚本工具）

---

## 4. 实施阶段与步骤

- [ ] **Phase 1: 抽离公共 Utils 模块**
  - 在 `OptimizeAndTool/Utils/UI/` 中整合所有通用的响应式绑定控件与构建器 (`UIBuild`, `UIItemSwitchBind`, `BindUIAVal` 等)。
  - 在 `OptimizeAndTool/Utils/Patch/` 中提炼通用的滚轮防切武器与 Shift 存取调度器。
- [ ] **Phase 2: 迁入各功能子域**
  - 迁入 `ReduceMouseLag` -> `Content/Optimize/ReduceMouseLag/`
  - 迁入 `VeinMining` -> `Content/QoL/VeinMining/`
  - 迁入 `PipetteTool` -> `Content/QoL/Pipette/`
  - 迁入 `SundryTool` -> `Content/Cheat/`
  - 迁入 `AccessoryBox` -> `Content/Storage/AccessoryBox/`
  - 迁入 `CreativeInventory` -> `Content/Creative/`
- [ ] **Phase 3: 统一 UI、快捷键与设置调度**
  - 在 `SettingUI.cs` 和 `Function.cs` 中整合所有功能开关与 UI 树。
  - 统一在 `ModifyInterfaceLayers.cs` 中注册绘制层与按键响应。
  - 统一在 `ModLinkage/` 中对接 `QuickButton` 与 `QuickSetting`。
- [ ] **Phase 4: 清理旧工程与解决方案**
  - 从 `tPlainModLoader.sln` 移除 6 个冗余工程。
  - 删除 6 个旧 Mod 目录。
  - 更新 `Mods/README.md`。
- [ ] **Phase 5: 全量构建与回归测试**
  - 执行 `dotnet build tPlainModLoader/tPlainModLoader.sln -c Release -m /graph`，确保 100% 通过（0 警告 0 错误）。
