# WandsTool 体验升级三阶段实施计划（对标 ImproveGame）

> **模组**：魔杖与建筑蓝图工具 (WandsTool)  
> **目标版本**：Terraria 1.4.5.7 / tPlainModLoader (`net472`)  
> **作者**：SaintCirno9  
> **日期**：2026-08-28

---

## 1. 背景与调研结论

对标 `TModSource/ImproveGame v1.8.2` 建造模块（CreateWand / MaterialCore / GenerateCore / TipRenderer）后的差距分析确认，第一梯队改进为：

1. **材料需求清单可视化** —— ImproveGame 在创造魔杖 tooltip 与 24 槽材料面板中实时展示"需求 vs 拥有 vs 可合成"；本模组目前仅在蓝图库中无任何材料预览（粘贴模式光标 tooltip 已有，但蓝图库卡片 hover 无感知），玩家在载入前无法评估成本。
2. **蓝图放置分帧协程 + 进度提示** —— ImproveGame `GenerateCore` 用 IEnumerator 每 50~60 格 `yield` 一帧并以 `TipRenderer` 在玩家头顶显示"放置中.../已放置"；本模组 `StructurePlacement.Place` 为**单帧同步执行 8 阶段**，大蓝图粘贴会明显卡顿，且过程无任何进度反馈。
3. **缺料虚影标红** —— 落点前把"放不下去"的格子以红色标识。`StructurePreview` 已有 `GetRequiredItems + GetInventoryHash` 的计划缓存基础设施，`CraftingPlan` 已能输出失败项，只差逐格映射与红色渲染。

**本模组已领先、无需改动的能力**（不纳入本次范围）：撤销+物料回滚（ImproveGame 无）、缺料自动合成引擎（ImproveGame 无）、剪切原子搬家、框选杂草过滤、Fusion 穿透取料、微光液体支持。

## 2. 调研确认的关键代码事实

| 事实 | 位置 | 对方案的影响 |
| --- | --- | --- |
| `CraftingPlan` 已输出 `DirectConsumes/CraftedCounts/MissingMessages` | `StructureCraftingEngine.cs:20-40` | 阶段1/3 复用其数据，仅补充 `MissingItemIds` |
| `GetInventoryHash` 背包指纹缓存已存在 | `StructureCraftingEngine.cs:642` | 阶段1/3 缓存键直接复用 |
| 粘贴模式光标 tooltip 已存在（含 6 行材料摘要） | `StructurePreview.cs:220-321` | 阶段1 不重复建设，聚焦蓝图库卡片悬浮 |
| `StructureData.GetRequiredItems` 含差量免除与多格家具锚点展开逻辑 | `StructureData.cs:249-358` | 阶段3 逐格掩码按同规则映射 |
| `UIBlueprintCard.RenderNormalView` 每次 Refresh 已对每卡片全量 `Load` 后丢弃 | `UIBlueprintCard.cs:50` | 阶段1 顺手保留该引用用于悬浮摘要，零额外 IO |
| 原版 `UIElement` 无 `Visible` 属性（1.4.4/1.4.5 反编译确认） | `GameSource/Terraria/Terraria.UI/UIElement.cs` | 阶段1 面板采用 UI 层 SpriteBatch 手绘（同 `DrawCursorModeTooltip` 风格），不引入 UIElement 显隐状态管理 |
| `StructurePlacement.Place` 仅有唯一调用点 | `Wands.cs:95` | 阶段2 可安全重构签名，无兼容包袱 |
| 卡片 Update 的 hover 检测已存在 | `UIBlueprintCard.cs:247-260` | 阶段1 悬浮通知挂在现有 Update 上 |

## 3. 阶段拆分与实现要点

### 阶段 1：蓝图库材料清单悬浮面板

**新增** `Content/Structure/StructureMaterialSummary.cs`：

- `MaterialEntry { ItemId, Required, Owned, State }`，State ∈ `Satisfied / Craftable / Missing`：
  - Required：`data.GetRequiredItems(null, true)`（蓝图库场景无落点，不做世界差量免除，口径为"总需求"）；
  - Owned：`GetPlayerInventorySnapshot`（含 Fusion 容器，比现有 tooltip 用的 `player.CountItem` 更准确）；
  - Craftable：`BuildPlan`（respect 自动合成配置）中 `CraftedCounts` 或 `IngredientConsumes` 可覆盖缺口；Missing：`MissingItemIds` 命中。
- 双缓冲缓存（蓝图引用 + `GetInventoryHash` + 三配置开关为键），hover 每帧调用也只算一次。
- `DrawOverlay(sb)`：光标右侧锚定深色面板（金边），逐行绘制物品图标（`ItemSlot.DrawItemIcon`，同 `Wands.DrawCursorModeTooltip` 规范）+ 名称 + `需求 x (拥有 y)`，行色 = 绿(满足)/金(可合成)/红(缺少)；最多 9 行 + "... 及其余 N 种"；底部汇总行；屏幕边缘钳制；异常静默（XNA 绘制线程防崩）。
- hover 存活判定用 `Main.GameUpdateCount` 帧戳（卡片 Update 内 `NotifyHover(data)` 刷新，2 帧内有效），管理器关闭/滚动后自动消失。

**修改** `UIBlueprintCard.cs`：`RenderNormalView` 保留已加载的 `StructureData` 到字段；`Update` 在正常视图 hover 时调用 `StructureMaterialSummary.NotifyHover`；重命名编辑态不触发。

**修改** `feces.cs`：UI 层 lambda 中 `UI?.Draw(...)` 之后调用 `StructureMaterialSummary.DrawOverlay(Main.spriteBatch)`。

### 阶段 2：蓝图放置分帧协程 + 进度提示

**重构** `StructurePlacement.cs`（唯一调用点 `Wands.cs:95` 同步迁移）：

- `Place(...)` → `BeginPlace(data, origin, player, overwrite, Action onSuccess) : bool`：
  - 前置材料校验与原子扣除（阶段 0/0.1，含剪切源区域清除）保持**同步一次性**执行，落格前材料账目不跨帧，杜绝扣料与放置分离导致的复制刷材料；
  - 8 阶段主体迁入 `IEnumerator PlaceRoutine(...)`，各阶段按操作预算分帧 `yield`（清理 400 格/帧、墙壁 5000、支撑 2500、Framing 800、家具 2000、电线涂层 10000、标牌 1500）；
  - `IsPlacing` / `Progress`（按已完阶段数+阶段内比例）/ `PhaseName` 静态暴露；`Update()` 由 `feces.DoUpdateInWorldPostfix` 每帧驱动，完成后播放音效、CombatText、剪切模式还原并回调 `onSuccess`（蓝图管理器自动重开逻辑由此接管）；
  - `Abort()`：魔杖关闭/回主菜单/玩家死亡时安全中止，已在 `feces` 既有清理分支与 `Wands.Update_Select` 死亡分支挂接；中止后世界保留已落格状态（说明见第 5 节）；
  - 放置期间：粘贴模式左键二次点击、右键取消、翻转快捷键全部忽略；`StructurePreview.Draw` 直接 return 不再绘制虚影（放置已在进行，虚影跟随鼠标会造成误导）。
- **进度提示**：`DrawProgress(sb)` 挂入 `Wands.Draw()`（Game 层），玩家头顶显示"放置中.. 45% (铺设支撑方块)"，动画省略号 + 黄色；完成后"已放置"绿色驻留 90 tick（对齐 ImproveGame TipRenderer 交互）。

### 阶段 3：缺料虚影标红

**修改** `StructureCraftingEngine.cs`：`CraftingPlan` 增加 `HashSet<int> MissingItemIds`，在三处缺口汇入点（免合成模式缺口、无配方、级联失败）登记目标 ItemID。

**修改** `StructurePreview.cs`：

- 计划缓存重建时同步构建 `bool[,] _missingMask`：按 `GetRequiredItems` 同款规则逐格判定（世界同物块免除、非覆盖占用跳过、多格家具锚点定料+整物展开、电线/促动器计入），命中 `MissingItemIds` 的格子置 true；`plan.IsPossible` 或免消耗时掩码为空，零开销；
- 墙壁/物块两趟渲染按掩码将 `Color.White * 0.55f/0.70f` 替换为红色调 `new Color(255, 80, 80) * 0.75f`；掩码随既有缓存键（鼠标格跨格/背包指纹/配置变更）刷新。

## 4. 不改动的相邻内容（明确排除）

- `StructurePreview.DrawMaterialTooltip` 现有 6 行光标摘要与其中的 emoji 字符（✔/🛠️/⚠️ 等既有产物，另行专项处理）；
- 结构操作接入撤销栈、90° 旋转、油漆模式、箱子内容捕获（第二/三梯队）；
- `ModConfig` 配置项不新增（三特性均复用现有开关语义：消耗物品/自动合成/工作台/覆盖/批量速率）。

## 5. 风险与对策

| 风险 | 对策 |
| --- | --- |
| 分帧期间世界/背包被外部改动（死亡、退出、其他模组） | 每帧 `Update` 前置卫语句（玩家死亡/魔杖关闭 → Abort + 提示）；材料已前置扣除，中止不回滚材料（与现有同步语义一致，仅时间窗拉长） |
| 掩码逐格判定在大蓝图 + 鼠标跨格时高频重算 | 与既有 `GetRequiredItems` tooltip 同量级（同键同频），且仅在 `!IsPossible` 时构建；可见性裁剪不变 |
| 悬浮面板遮挡卡片操作 | 光标右侧偏移 + 屏幕钳制；面板为纯绘制不拦截鼠标（不进入 UIElement 树） |
| `BeginPlace` 后 `AutoReopenManagerAfterPlacement` 语义漂移 | 成功回调内保持原判断分支（flag 仍为真才重开并复位），取消路径不动 |
| 多人联机 | 完成时按阶段 7 一次性 `SendTileSquare`；中止路径（死亡/关魔杖/切模式）在作业已落格时补发一次区域同步，避免本地与服务器不一致 |

## 6. 验证与交付

1. 每阶段完成后：`dotnet build tPlainModLoader/Mods/WandsTool/WandsTool/WandsTool/WandsTool.csproj -c Release`，0 错误 0 警告为准入（自动热部署）；
2. 三阶段全部完成后更新 `WALKTHROUGH.md`（新增第 5 节实施记录）；
3. 建议实机回归点：蓝图库 hover 摘要与缓存命中率、大蓝图（≥200×200）粘贴帧率与进度条、缺料蓝图虚影红色范围与 `MissingMessages` 一致性、放置中右键/左键/翻键拦截、死亡与关魔杖中止后的世界状态。
