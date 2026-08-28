# 魔杖与建筑蓝图工具 (WandsTool) 架构设计与实施验收

> **模组名称**：魔杖与建筑蓝图工具 (WandsTool / Structure Blueprint System)  
> **适用版本**：Terraria 1.4.5.7 / tPlainModLoader (`net472`)  
> **作者**：SaintCirno9  

---

## 1. 功能概述与核心体验

`WandsTool` 为 `tPlainModLoader` 提供了原版级的物块/墙壁批量建造魔杖以及专业级的建筑蓝图复制、剪切、翻转、放置与库管理系统：

1. **物块/墙壁模式鼠标光标提示 (100% 对齐原版渲染管线)**：
   - 彻底移除冗余的“魔杖:”前缀，采用原版 `ItemSlot.DrawItemIcon` 渲染光标右下跟随贴图；
   - 手持武器启动魔杖时，自动从背包首个槽位检索有效物块/墙壁；材料耗尽时显示红色警示 `[缺材料]`。
2. **建筑框选智能过滤**：
   - 基于 `Main.tileCut` 与环境装饰判定（蘑菇、碎石堆、钟乳石、海藻水草等），在框选复制/剪切时自动过滤自然杂草与地表碎屑，保留主体建筑结构。
3. **精准半砖、斜坡与平台翻转系统**：
   - **实体物块**：支持原版 4 种斜坡（1~4）与下半砖（5）/垂直翻转上半砖（6）虚影渲染与真实放置；
   - **平台（Platforms）**：平台半砖（5）下移 8px 绘制完整横杠切片，垂直翻转时普通顶部平台与半砖平台精准互换（`0 <-> 5`），平台楼梯切片（`144 <-> 126`、`198 <-> 162`、`324 <-> 306`）左右对称镜像。
4. **建筑蓝图管理器与通用输入框组件 (Blueprint Library & General TextBox Component)**：
   - 采用标准紧凑字号 `MouseText (0.85f)`，彻底移除原版不支持的 emoji 字符以防乱码；
   - 提炼并升级框架底层通用 `tContentPatch.Content.UI.UITextBox` 输入框组件，彻底修复 UI 坐标判断与失焦判定、提供 Windows IME 拼音合成与闪烁光标支持、支持超长文本平滑水平视口滚动；
   - 蓝图重命名与顶部蓝图库均接入通用输入框，支持回车（Enter）直接确认保存、Esc 取消编辑、实时名称与文件名关键词过滤。
5. **蓝图载入放置智能视图隐藏与闭环重新显示 (Placement QoL)**：
   - 在蓝图管理器中点击【载入放置】后，系统自动主动隐藏蓝图管理器与魔杖轮盘，将完整无遮挡的视野让渡给玩家进行虚影预览与落点摆放；
   - 在玩家鼠标左键**放置成功**或鼠标右键**取消放置**后，系统自动重新显示蓝图管理器面板，形成无缝闭环的蓝图操作体验。
6. **Loader 核心级文本输入全局静默**：
   - 将文本输入防冲突逻辑下沉至 Loader 底层 `tContentPatch.Input.ModKeybind`；
   - 在任何输入框获得焦点或打字时，系统全局拦截并静默所有 `ModKeybind`，彻底避免打字误触发巨大背包、吸管工具等快捷键。
7. **背包开启常驻与光标抓取识别**：
   - 打开背包不再强制关闭魔杖，鼠标在世界空白区可直接框选建造，悬停背包 UI 控件上时正常操作物品；
   - 鼠标光标抓取的物品（`Main.mouseItem`）被识别为最高优先级的魔杖材料，框选时直接消耗光标堆叠并拦截 `Player.DropSelectedItem` 防止误丢弃整组材料。

---

## 2. 核心架构与模块划分

```
WandsTool/
├── WandsTool.csproj                    # SDK 风格 MSBuild 工程文件（含 DeployToGameDir）
├── feces.cs                            # 模组主生命周期与 UI 钩子（继承 PatchMain）
├── ListenInput.cs                      # 本地按键输入监听
├── Content/
│   ├── Wands/
│   │   ├── Wands.cs                    # 建造魔杖核心轮询、光标跟随提示渲染、选区与形状分发
│   │   ├── WandAction.cs               # 魔杖物块/墙壁放置与队列执行动作（Fusion 穿透取料/存料）
│   │   ├── WandUtils.cs                # 几何形状算法（直线/空心圆/实心圆/实心矩形/空心矩形）与绘制
│   │   ├── WandHistory.cs              # 施工一键撤销快照栈（上限 30 步 + 物料智能回滚）
│   │   └── WandPreview.cs              # 半透明材质施工虚影渲染器（视口裁剪，放置/破坏/液体）
│   ├── Structure/
│   │   ├── TileSnapshot.cs             # 单格图元快照（物块、墙壁、斜坡、电线、涂料，含杂草过滤）
│   │   ├── StructureData.cs            # 结构数据容器（含 Framing 贴图切片与坡度水平/垂直镜像）
│   │   ├── StructureStorage.cs         # 本地蓝图序列化、反序列化与重命名文件管理器
│   │   ├── StructurePlacement.cs       # 结构放置阶段式执行器（墙壁 -> 物块/斜坡 -> 家具/实体对象）
│   │   ├── StructurePreview.cs         # 原版级 8 组细条封边斜坡与半砖虚影实时预览渲染
│   │   └── UI/
│   │       ├── UIBlueprintManager.cs   # 蓝图库管理器主窗口
│   │       └── UIBlueprintCard.cs      # 蓝图条目卡片（支持载入放置、原地重命名、删除）
│   └── UI/
│       ├── UIWandsPanel.cs             # 魔杖主轮盘操作面板
│       └── UIStructurePanel.cs         # 蓝图结构操作面板
└── ModLinkage/
    └── ModQuickButton.cs               # 悬浮工具栏 (QuickButton) 联动
```

---

## 3. 验证与构建测试

1. **单 Mod 快速编译与部署**：
   ```pwsh
   dotnet build tPlainModLoader/Mods/WandsTool/WandsTool/WandsTool/WandsTool.csproj -c Release
   ```
   - 耗时 ~2.0s，0 错误，0 警告；
   - 自动生成 `WandsTool.dll` 并热部署至 `$(TerrariaDir)\tPlainModLoader\Mods\WandsTool\`。

2. **tPlainModLoader 全量解决方案静态图构建**：
   ```pwsh
   dotnet build tPlainModLoader/tPlainModLoader/tPlainModLoader.sln -c Release -m /graph
   ```
   - 除 `OptimizeAndTool` 工程（含其它会话进行中的未完成 WIP，`InfinitePotionAndBuff.cs` 存在既有的 `MouseTextHackZoom` 重载调用编译错误）外全部构建通过；WandsTool 自身 0 错误 0 警告。

---

## 4. 体验升级与功能拓展专项（实施记录）

> 目标：解决"打开物品栏时魔杖失效、无法绘制选区"核心痛点 + 4 项扩展能力。  
> 版本：Terraria 1.4.5.7 / tPlainModLoader (`net472`)

### 4.1 背包开启共存与光标抓取识别

- **`feces.cs`**：移除 `Main.playerInventory != lastPlayerInventory` 强制退出魔杖逻辑（含 `lastPlayerInventory` 字段），魔杖状态跨背包开关保持常驻。
- **`Wands.cs`**：蓝图粘贴与常规框选启动条件全部移除 `Main.playerInventory` 阻断，仅保留 `mouseInterface`、`editChest`、`editSign`、`ingameOptionsWindow`、`drawingPlayerChat` 等真实 UI 输入状态拦截；背包开启时鼠标在世界空白区按下左键即可启动框选。
- **`WandAction.cs`**：`FirstItem_TileOrWall` 首选优先级提升为 `Main.mouseItem`（光标抓取物），放置成功经统一 `ConsumeMaterial` 同步扣除光标堆叠，耗尽自动 `TurnToAir()`。
- **`Patch_PlayerAction.cs`**：新增 `Player.DropSelectedItem` Harmony 前缀补丁，魔杖模式下世界场景一律拦截丢弃（背包开启且鼠标悬停 UI 控件时放行，保证槽位整理正常）。

### 4.2 背包融合 (Fusion) 穿透取料与回收

- `FirstItem_TileOrWall` 在主背包检索无果后，遍历 `InventoryFusionManager.GetActiveSources(player)` 查找可放置物块/墙壁并记录来源；
- 扣费经 `ConsumeMaterial`：Fusion 源物品扣减后标记脏源，队列处理完毕由 `FlushFusionDirty` 统一调用 `OnModified` 持久化（避免逐格高频写盘）；
- `TryConsumeLiquidBucket` 支持从 Fusion 容器检索无底桶与普通液体桶；
- `GiveItemToPlayer` 升级为 `public static`：主背包满时优先注入激活 Fusion 容器（同类堆叠 -> 空格），最后才 `QuickSpawnItem` 掉落，彻底消除掉落物风暴。

### 4.3 施工一键撤销 (Undo System)

- **新增 `Content/Wands/WandHistory.cs`**：`WandTileSnapshot` 单格快照（物块类型/坡度半砖/背景墙/液体/电线/油漆），`WandActionRecord` 记录快照集 + `Dictionary<int,int> consumedItems`，`LinkedList` 上限 30 步；
- 操作入队前 `WandHistory.BeginRecord` 抓取选区全量"操作前"状态；队列清空后 `CheckFinalize` 比对世界判定实际变化再归档（无变化自动丢弃）；
- 撤销 `Undo` 逐格全新 `Tile` 重建还原（含油漆/电线/促动器），多人模式下 `NetMessage.SendTileSquare` 广播，随后将 `consumedItems` 物料经 `GiveItemToPlayer` 返还主背包/Fusion 容器；撤销破坏操作免费复原不二次扣费；
- **`WandsKeybind.cs`**：注册 `UndoAction` 快捷键（默认 `U`，可原版设置改键）；**`feces.cs`** 监听触发并弹出格数反馈。

### 4.4 空心/实心几何图形拓展

- `Wands.Shapes` 枚举扩展为 5 种：`line / circular / filledCircular / rectangle / hollowRectangle`；
- `WandUtils` 新增 `GetShapes_FilledCircular`（椭圆方程 `dx²/a² + dy²/b² <= 1` 逐行扫描）与 `GetShapes_HollowRectangle`（1 格厚度外框点集），及对应 `Draw_filledCircular` / `Draw_hollowRectangle` 绘制；
- `wandsPanel.cs` 形状子菜单 3 -> 5 按钮（新增实心圆、空心矩形图标），布局环绕 96px 5 等分；新增 `Resources/ShapesFilledCircular.png`、`ShapesHollowRectangle.png`（16x16 青色矢量风格与既有图标一致）；
- `Wands.DrawCursorModeTooltip` 形状标注更新为 `[线]/[空心圆]/[实心圆]/[实心矩形]/[空心框]`。

### 4.5 半透明材质施工虚影预览

- **新增 `Content/Wands/WandPreview.cs`**：拖拽框选阶段实时渲染——
  - 放置模式：目标格材质（`TextureAssets.Tile/Wall`）50% 透明 + 淡绿微光底色，空白格按 `placeStyle` 估算标准帧；
  - 破坏模式：既有物块/背景墙 40% 红色高亮遮罩 + 对角裂纹细线；
  - 液体模式：按液体类型色（水蓝/熔岩橙/蜂蜜黄/微光紫/青/灰白）渲染 45% 半透明水体 + 液面高光边；
  - 严格视口裁剪（仅绘制 `Main.screenPosition` 可视范围图块）+ 点集哈希缓存，超大框选保持满帧。

### 4.6 实施偏差与注意事项

1. **Fusion 扣料接口**：计划中提及 `IFusionItemSource.ModifySlot`，实际框架接口仅提供 `GetSlots` + `OnModified`（与 `StructureCraftingEngine` 现有用法一致），故采用"直接扣减槽位堆叠 + 队列完毕统一 `OnModified`"方案。
2. **Undo 快捷键**：KeybindLoader 支持单键字符串绑定，默认键采用 `U`（计划中"或独立按键 U"分支），不支持 `Ctrl+Z` 组合键表达。
3. **撤销时机**：批量操作队列入队到处理完毕跨帧进行，队列未空时按撤销键会提示"上一次操作尚未处理完成"，避免与批处理写格冲突。
4. **范围收敛**：按计划仅对物块/墙壁放置、破坏与液体操作接入 Undo；电线、结构删除/复制/剪切/粘贴模式不进撤销栈（快照数据仍完整记录电线位）。

### 4.7 验证与构建结果

1. **单 Mod 快速构建**：`dotnet build ...WandsTool.csproj -c Release` —— 0 错误 0 警告，自动热部署至 `$(TerrariaDir)\tPlainModLoader\Mods\WandsTool\`。
2. **全量解决方案构建**：除 `OptimizeAndTool`（他会话进行中 WIP，`MouseTextHackZoom` 调用为既有编译错误，与本次改动无关）外全部通过；公共框架工程（TPML.Core / tContentPatch / TPML.Content / CommandHelp）与 WandsTool 均构建成功。
3. **建议实机回归点**（GABS / 手动）：背包开启框选放置、光标抓物铺设、Fusion 存钱罐取料、30 步内撤销还原与物料返还、实心圆/空心矩形生成、五形状虚影预览帧率。

---

## 5. 对标 ImproveGame 体验升级三阶段（实施记录）

> 目标：蓝图材料清单可视化、蓝图放置分帧协程 + 进度提示、缺料虚影标红（详见 `IMPLEMENTATION_PLAN.md`）。  
> 版本：Terraria 1.4.5.7 / tPlainModLoader (`net472`)

### 5.1 阶段一：蓝图库材料清单悬浮面板

- **新增 `Content/Structure/StructureMaterialSummary.cs`**：材料需求计算器 + UI 层手绘悬浮面板。
  - 三态判定：满足(绿) / 可自动合成(金) / 缺少(红)；需求口径为 `GetRequiredItems(null, true)` 总需求（蓝图库无落点，不做世界差量免除），"拥有"采用 `GetPlayerInventorySnapshot`（含 Fusion 容器，比粘贴模式 tooltip 用的 `player.CountItem` 主背包口径更准确）；
  - 双缓冲缓存键：蓝图引用 + `GetInventoryHash` 背包指纹 + 消耗/自动合成/工作台三开关，悬停每帧调用仅重算一次；
  - 面板绘制：光标右侧锚定金边深色面板，逐行 `ItemSlot.DrawItemIcon` 物品图标 + `需求/拥有` 文本（最多 9 行 + 折叠行），底部汇总行；屏幕边缘钳制翻转；整体 try/catch 静默容错防 XNA 绘制线程崩溃；
  - 存活判定：`Main.GameUpdateCount` 帧戳（卡片 Update 内 `NotifyHover` 刷新，>2 帧未刷新自动消失），不进入 UIElement 树、不拦截鼠标。
- **`UIBlueprintCard.cs`**：`RenderNormalView` 将已加载的 `StructureData` 保留至 `_loadedData` 字段（零额外 IO）；正常视图悬停时调用 `NotifyHover`；重命名编辑态（`_isEditing`）不触发。
- **`feces.cs`**：UI 层 lambda 在魔杖轮盘绘制后调用 `StructureMaterialSummary.DrawOverlay`。

### 5.2 阶段二：蓝图放置分帧协程 + 进度提示

- **`StructurePlacement.cs` 重构**（原单帧同步 `Place` → 分帧协程，唯一调用点 `Wands.cs` 同步迁移）：
  - `BeginPlace(data, origin, player, overwrite, onSuccess)`：材料校验与原子扣除、剪切源区域清除保持**同步一次性**执行（落格前材料账目不跨帧，杜绝复制刷材料）；八阶段主体（清理→背景墙→支撑→Framing→家具→电线涂层→网络同步→标牌）迁入 `IEnumerator PlaceRoutine`，按操作预算分帧（清理 150/帧、墙 800、支撑 400、Framing 150、家具 200、电线 2000、标牌 300）；
  - 进度跟踪：已完成阶段数 + 当前阶段内比例 → `Progress`（0~1）；`PhaseName` 暴露当前阶段名；`Update()` 由 `feces.DoUpdateInWorldPostfix` 每帧驱动，`MoveNext` 完结后置 `FinishTipCountdown=90` 并回调 `onSuccess`（蓝图管理器自动重开逻辑由此接管）；
  - 安全卫语句：玩家死亡/回主菜单自动 `Abort()`（已落格部分保留，材料不回滚）；魔杖关闭时 `feces` 清理分支调用 `Abort()`；
  - 放置期间输入封锁（已收窄，见 5.6）：粘贴模式左键重复放置与右键取消被忽略、右键轮盘 Toggle 拦截、翻转快捷键忽略、快捷栏切换不触发 `AutoAdaptModeToHeldItem`、撤销键封锁；鼠标 release 标志不消费，背包/合成等原版 Draw 阶段 UI 点击不受影响；`StructurePreview.Draw` 直接跳过（虚影跟随鼠标会误导落点）。
- **进度提示 `DrawProgress`**：挂入 `Wands.Draw()`（Game 层），玩家头顶显示黄色"放置中.. 百分比 (阶段名)"带动画省略号，完成后绿色"已放置"驻留 90 tick（对齐 ImproveGame TipRenderer 交互）。

### 5.3 阶段三：缺料虚影标红

- **`StructureCraftingEngine.cs`**：`CraftingPlan` 新增 `HashSet<int> MissingItemIds`，在三处缺口汇入点（免合成缺口、无配方、级联失败）登记目标物品 ID。
- **`StructurePreview.cs`**：
  - 计划缓存逻辑提取为共享 `TryGetPlan(data, mouseTile)`（原 `DrawMaterialTooltip` 内联缓存块迁移，缓存键增加免消耗模式短路）；
  - 计划重建时同步构建 `bool[,] _missingMask`（`BuildMissingMask`）：按 `GetRequiredItems` 同款规则逐格判定（世界同物块免除、非覆盖占用跳过、多格家具锚点定料 + 整件标红、电线/促动器计入），命中 `MissingItemIds` 置位；`plan.IsPossible` 或免消耗时掩码为 null 零开销；
  - 墙壁/物块两趟渲染按掩码以红色调（`new Color(255,80,80) * 0.55f/0.45f/0.75f`）替换原半透明白色渲染。

### 5.4 实施偏差与注意事项

1. **阶段一范围收敛**：调研发现粘贴模式光标物料 tooltip 已存在（`StructurePreview.DrawMaterialTooltip`），悬浮面板聚焦蓝图库卡片 hover 场景，两处材料摘要并存、口径不同（卡片面板含 Fusion，粘贴 tooltip 仅主背包）——现有 tooltip 保持原样未动，后续可统一。
2. **面板实现形式**：原版 `UIElement`（1.4.4/1.4.5 反编译确认）无 `Visible` 属性，且 `WandsTool.Content.Structure` 命名空间在 `feces.cs` 存在名称解析冲突（CS0103），故面板采用 UI 层 SpriteBatch 手绘 + 全限定名引用，未引入 UIElement 显隐状态管理。
3. **C# 7.3 语法约束**：工程 LangVersion 为 7.3，switch 表达式不可用，`StructureMaterialSummary` 采用三元辅助方法替代。
4. **放置中止语义与时长模型**（初版估算有误，已按审查模型修正）：协程中止（死亡/关魔杖/回主菜单/切模式）保留已落格世界状态，材料不回滚——与原同步实现的"全有或全无"存在差异。分帧时长按"格数 ÷ 阶段预算"估算：120×120 约 1 秒、200×200 约 3~4 秒、500×500 约 20~25 秒 @60fps（初版"2~4 秒"公式量纲错误）。作为补偿，放置期间鼠标 release 标志不消费，背包/合成等原版 UI 点击全程可用；联机下中止会补发区域图格同步。
5. **既有 emoji 残留**：`DrawMaterialTooltip` 中 ✔/🛠️/⚠️ 等字符为既有产物，本次未触碰（WALKTHROUGH 4.4 已注明原版不支持部分 emoji，属历史遗留，另行专项处理）。

### 5.5 验证与构建结果

1. **单 Mod 快速构建**：`dotnet build tPlainModLoader/Mods/WandsTool/WandsTool/WandsTool/WandsTool.csproj -c Release` —— 每阶段 0 错误 0 警告，自动热部署至 `$(TerrariaDir)\tPlainModLoader\Mods\WandsTool\`。
2. **全量解决方案构建**：`dotnet build tPlainModLoader/tPlainModLoader/tPlainModLoader.sln -c Release -m /graph` —— 0 错误 0 警告（含 OptimizeAndTool，前次记录的 WIP 编译错误已不存在）。
3. **建议实机回归点**（GABS / 手动）：蓝图库卡片悬停材料面板与背包变更后刷新、缺料蓝图虚影红色范围与粘贴 tooltip `MissingMessages` 一致性、≥200×200 大蓝图粘贴帧率与进度条阶段推进、放置中左键/右键/翻键/切物品栏封锁、剪切搬家中止后的世界状态、免消耗模式下掩码与面板零渲染路径。

### 5.6 审查修复记录（第二轮）

> 依据独立审查子代理报告（安全/效率双维度）执行修复；严重度分级沿用报告口径。

**高-1 放置期间输入封锁面过宽 + 时长模型修正（已修复）**
- 原实现 `IsPlacing` 期间每帧无条件清空 `Main.mouseLeftRelease/mouseRightRelease`，导致放置全程（大蓝图可达数十秒）原版背包/合成/装备栏等 Draw 阶段 UI 点击全部失效（原版点击判定依赖这两个标志且发生在 Draw 阶段）。修复：`Wands.Update_Select` 粘贴分支与 `feces` 右键 Toggle 分支均改为"忽略魔杖侧输入但不吞标志"，放置期间原版 UI 与世界右键交互正常可用；
- 分帧预算整体上调约 5~12 倍（清理 400/帧、墙 5000、支撑 2500、Framing 800、家具 2000、电线 10000、标牌 1500），200×200 约 3~4 秒、500×500 约 20~25 秒 @60fps（初版文档"2~4 秒"为量纲错误，已修正）。

**中-1 剪切搬家模式掩码误报（已修复）**：`StructurePreview.TryGetPlan` 头部对 `CutSourceRect.HasValue` 短路返回并清空计划与掩码——剪切搬家零消耗，不再整幅虚影标红，也不再跨格空算 `BuildPlan`。

**中-2 放置中模式可被面板切换（已修复）**：`StructurePlacement.Update` 新增模式卫语句，作业期间 `Wand_StructureMode != Paste` 即 `Abort()` 并弹"放置已中止"CombatText，防止用户操作与协程写格交错及剪切收尾强行覆盖用户所选模式。

**中-3 联机中止不广播（已修复）**：`Abort()` 在 `netMode == 1` 且作业已开始落格（阶段计数非零）时补发一次区域 `SendTileSquare`，消除本地与服务器状态不一致窗口。

**中-4 撤销键未封锁（已修复）**：`feces` U 键分支新增 `IsPlacing` 卫语句，放置中按 U 提示"蓝图放置进行中，暂不能撤销"。

**低严重度顺手修复**：悬浮面板回主菜单清空悬停与缓存引用（防蓝图数据滞留）；`EnsureComputed` 异常置 `_computeFailed`，汇总行显示"材料清单计算失败"替代误导性的"材料齐备"；`DrawProgress` 补 try/catch 与 `DrawOverlay` 容错口径对齐；关魔杖中止时弹"放置已中止"提示（对齐计划承诺）；`StructurePlacement.CountItemInInventory` XML 注释修正为"仅主背包（0~57），不含 Fusion 容器"（既有注释与实现不符，非本次引入）。

**审查判定为误报/可接受、未改动项**：卡片 `_loadedData` 常驻内存（功能必需，量级随库规模）；悬停帧戳竞态、缓存键完整性、越界索引、SendTileSquare 巨包、标牌顺序、掩码效率等均经核对不成立或属既有行为；空文本标牌占位错位为既有瑕疵，另行专项处理。

**修复后验证**：单 Mod 构建 0 错误 0 警告；全量解决方案 `-m /graph` 构建通过。新增实机回归点：放置中开启背包点击物品槽/合成、放置中切换模式（应中止并提示）、联机放置中死亡后检查服务器同步。
