# AccessoryBox 饰品箱重构设计与实施验收文档

## 1. 重构背景与定位

- **原版痛点**：旧版 `AccessoryBox` 仅通过手持物品点击进行虚拟记录（复制物品，无法正常拿取与交互），无法作为随身容器使用。
- **重构目标**：参考 `OptimizeAndTool` 中的 `BigBag`（巨大额外背包）最新架构，将 `AccessoryBox` 彻底重构为**兼具「真实实体收纳容器」与「饰品属性挂载器」双重特性的强大随身装备库**。

---

## 2. 核心特性与架构升级

### 2.1 真实的物品存储与交互体系 (同构 BigBag)
- **实体存取**：存储槽数组 `Item[] Slots`（支持 40~500 格配置，默认 100 格），存入真实消耗背包物品，取出真实放回鼠标/背包；
- **鼠标完整交互 (`BoxItem.cs`)**：
  - **左键**：空手拿起整堆 / 手持放下 / 同类合并堆叠 / 物品交换；
  - **Shift + 左键**：`Main.LocalPlayer.GetItem(..., QuickTransferFromSlot)` 快速转移回玩家背包；
  - **右键**：空手取一半（`(stack+1)/2`）/ 手持逐个放置（`stack++`）；
  - **悬停与渲染**：`ItemSlot.MouseHover` 显示完整原版 Tooltip，`ChatManager.DrawColorCodedStringWithShadow` 绘制堆叠数字，标记 `mouseInterface = true` 防止点击穿透。
- **背包 Shift 快捷存入补丁 (`Patch_BoxShiftTransfer.cs`)**：
  - 当饰品箱打开时，玩家在个人背包中按住 Shift 悬停在物品上，光标自动高亮为 `TransferToChest`（9号箱子转移光标），点击一键将背包物品转移放入饰品箱。

### 2.2 顶部便捷操作工具栏 (`BoxWindow.cs`)
- **一键存入 (`DepositAllFromPlayer`)**：一键将玩家个人背包 10~49 格（跳过快捷栏 0~9）非收藏物品存入饰品箱并自动堆叠；
- **快速堆叠 (`QuickStackFromPlayer`)**：一键向箱内已有同类物品快速补齐堆叠；
- **一键取出 (`LootAllToPlayer`)**：一键将箱内所有物品转移至玩家背包；
- **智能整理排序 (`SortAccessoryBox`)**：合并同类未满堆叠，并按「饰品 > 防具 > 武器 > 工具 > 消耗品 > 材料 > 杂项」智能归类排序；
- **饰品属性挂载一键开关**：顶部提供独立切换按钮，实时切换被动生效状态并在悬停时提示当前状态，支持高亮/变灰显示。

### 2.3 饰品与防具属性挂载被动生效 (`AccessoryBox.cs`)
- 在 `UpdateEquipsPostfix` 钩子中：
  - 只要 `EnableMod` 与 `EnablePassive` 开启，箱内所有有效装备/饰品持续为玩家生效：
    - 前缀词条加成（`GrantPrefixBenefits`，如 护佑 +4防御、险恶 +4%伤害）；
    - 基础护甲与防具属性（`GrantArmorBenefits`）；
    - 功能饰品被动效果全量挂载（`ApplyEquipFunctional`）；
    - 翅膀飞行逻辑与时装外观处理（`wingsLogic`、`wings`、`ApplyEquipVanity`）。

### 2.4 界面体验与滚轮平滑支持
- **平滑滚动面板 (`UIBoxWrapPanel`)**：自适应计算总高度，集成 `UIScrollbar` + `UIList`；
- **滚轮防穿透 (`Patch_HotbarScroll.cs`)**：悬停在饰品箱窗口内时，拦截快捷栏滚轮事件，防止误切换快捷栏，同时驱动箱子列表顺畅滑动；
- **背包联动**：打开饰品箱时自动呼出玩家个人背包（`Main.playerInventory = true`），按 ESC / E 关闭背包时饰品箱同步关闭。

### 2.5 数据持久化与统一快捷键
- **数据持久化 (`AccessoryBoxStorage.cs`)**：通过 `ModSetting` 将数据保存至 `accessoryBox.json`（保存 `type`, `prefix`, `stack`），自动迁移兼容旧数据；
- **统一快捷键 (`AccessoryBoxKeybind.cs`)**：注册 `ToggleAccessoryBox` 快捷键，支持游戏内改键；
- **联动呼出 (`ModQuickButton.cs`)**：保持与 `QuickButton` 快捷按钮的联动调用。

---

## 3. 构建与部署验收结果

- **构建命令**：`dotnet build tPlainModLoader/Mods/AccessoryBox/AccessoryBox/AccessoryBox/AccessoryBox.csproj -c Release`
- **构建状态**：0 警告，0 错误，耗时 ~1.9s，产物自动部署至游戏目录 `Terraria/tPlainModLoader/Mods/AccessoryBox/`。
- **全量解决方案构建**：`dotnet build tPlainModLoader/tPlainModLoader/tPlainModLoader.sln -c Release -m /graph`，20 个项目全量构建通过（~3.7s）。
