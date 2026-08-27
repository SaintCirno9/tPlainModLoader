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
