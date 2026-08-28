# RecipeBrowser (合成表与物品图鉴) 移植实施与架构验收文档

本文档记录 **RecipeBrowser** 从 tModLoader 迁移移植至 **tPlainModLoader (TPML)** 原版生态 (Track B 原生模组) 的完整技术架构、核心实现与验证结果。

---

## 1. 模组概述与移植定位

- **原模组**：`RecipeBrowser` v0.12 (Steam 创意工坊 ID: 2619954303)
- **目标运行环境**：.NET Framework 4.7.2 (x86) / 官方原版 Terraria 1.4.4+
- **作者**：`SaintCirno9`
- **代码位置**：`tPlainModLoader/Mods/RecipeBrowser`
- **核心定位**：提供全功能的配方图鉴（Recipes）、多层合成路线（Craft）、物品图鉴（Items）、生物图鉴与掉落（Bestiary）以及游戏内快捷查询/收藏夹面板。

---

## 2. 核心架构与关键技术实现

### 2.1 零反射强类型访问 (Publicizer)
- 借助 TPML 全局配置的 `Krafs.Publicizer`，直接使用强类型语法访问原版核心类型与内部成员（`Recipe.UpdateRecipeList()`, `RecipeGroup`, `ContentSamples.ItemsByType`, `ItemDropDatabase` 等），完全移除所有运行时反射调用，实现 100% 静态类型安全与零反射开销。

### 2.2 数据持久化：Sidecar 存储无缝集成
- **玩家收藏夹与已见图格数据**：通过继承 `TPML.Content.ModPlayer`，由 `TPML.Content.Core.ModItemSidecarEngine` 全自动将玩家数据保存在伴随存档 `TPML_Saves/Player_<PlayerName>.tpml_data`：
  - `StarredRecipes`: 收藏配方的完整结构快照（通过 `RecipeIO` 进行序列化与反序列化）；
  - `SeenTiles`: 玩家已探索/已见到的制作站 Tile 索引快照。
- **纯客户端配置**：窗口拖拽坐标、缩放尺寸、收藏夹悬浮窗位置等保存在 TPML 标准用户配置目录：
  `Documents/My Games/Terraria/tPlainModLoader/Config/RecipeBrowser/config.json`。

### 2.3 背包聚合融合 (Inventory Fusion) 穿透检测
- 在 `RecipePath.cs`、`RecipeCatalogueUI.cs` 和 `UIRecipeSlots.cs` 中深度融合 `TPML.Content.Fusion.InventoryFusionManager.GetAllFusionItems(Main.LocalPlayer)`：
  - 计算可合成配方（`AbleToCraft` 与 `AbleToCraftExtended`）时，自动穿透检索外部扩展容器（如大背包 `BigBag`、药水袋等）；
  - 配方原料直观显示材料存量与聚合扣减。

### 2.4 文本标签与富文本聊天栏渲染
- 注册 `LinkTagHandler` (`[l/url:text]`)、`ImageTagHandler` (`[image/scale:path]`)、`NPCTagHandler` (`[npc:id]`)、`ItemHoverFixTagHandler` (`[itemhover:id:stack:check]`)，完全兼容原版 `ChatManager` 与 `TextSnippet` 体系。

### 2.5 统一热键与按键总线 (KeybindLoader)
- 通过 `tContentPatch.Input.KeybindLoader` 注册 3 个核心快捷键：
  - `O`: 打开/关闭合成表 (Toggle Recipe Browser)
  - `C`: 查询鼠标悬停物品 (Query Hovered Item)
  - `F5`: 切换收藏夹悬浮面板 (Toggle Favorited Recipes Window)

---

## 3. 构建与部署验证

全量 18 个工程已成功完成多核并发构建，编译结果为 **0 个警告，0 个错误**：

```powershell
dotnet build tPlainModLoader/tPlainModLoader/tPlainModLoader.sln -c Release -m /graph
```

- **输出目标**：`tPlainModLoader/Mods/RecipeBrowser/RecipeBrowser/bin/Release/net472/RecipeBrowser.dll`
- **自动部署**：自动复制并部署至 `C:\Games\Steam\steamapps\common\Terraria\tPlainModLoader\Mods\RecipeBrowser`
- **资源内嵌**：67 个 UI 贴图 PNG 与 HJSON 汉化文件完整打包入库。

---

## 4. 核心性能重构与稳定性强化记录

### 4.1 毫秒级性能爆发重构 (566ms -> 2.7ms 极速响应)
- **根因分析**：Profiler 诊断表明耗时集中在配方网格排序（QuickSort 比较 35,000 次）。默认排序 `RecipeOrder` 原先调用了 `Array.IndexOf(Main.recipe, x)`，单次排序触发了 **2.1 亿次数组线性遍历**，导致点击卡顿 565ms+。
- **优化措施**：
  1. 移除 `Array.IndexOf`，在 [`ItemGridSort`](file:///c:/Users/loris/Documents/Cirno9TerrariaMods/tPlainModLoader/Mods/RecipeBrowser/RecipeBrowser/RecipeCatalogueUI.cs) 中直接利用槽位强类型索引 `r1.index.CompareTo(r2.index)` 实现 **$O(1)$ 单次整型比对**；
  2. 附近宝箱材料扫描通过 `RefreshNearbyChestCache()` 单帧哈希预计算，消灭全图 1000 个宝箱的暴力循环；
  3. 耗时从 **566.27ms 暴降至 2.72ms（提升 200+ 倍）**，达到瞬时响应。

### 4.2 XNA 裁剪异常（ScissorRectangle Invalid / Cannot call Present）根治
- **根因分析**：Tab 切换时新面板尺寸尚未完成首帧重算（Width/Height=0），原版 `UIElement.GetClippingRectangle` 向 XNA 底层传入了非法裁剪矩形，抛出 `The scissor rectangle is invalid` 并引发主循环 `Cannot call Present when a render target is active` 级联崩溃。
- **优化措施**：
  1. 在 [`RecipeBrowserMod.Load`](file:///c:/Users/loris/Documents/Cirno9TerrariaMods/tPlainModLoader/Mods/RecipeBrowser/RecipeBrowser/RecipeBrowserMod.cs) 中激活 Harmony 补丁集；
  2. 在 [`Patches.cs`](file:///c:/Users/loris/Documents/Cirno9TerrariaMods/tPlainModLoader/Mods/RecipeBrowser/RecipeBrowser/Patches.cs) 中注入 `UIElement.GetClippingRectangle` 全局安全防护，自动探测当前活动 `RenderTarget2D` / 视口并强制钳位有效尺寸（`Width/Height >= 1`）；
  3. 在 [`TabController.SetPanel`](file:///c:/Users/loris/Documents/Cirno9TerrariaMods/tPlainModLoader/Mods/RecipeBrowser/RecipeBrowser/TabController.cs) 中解绑旧父容器并立即执行 `parent.Recalculate()`。

### 4.3 制作树递归构造异常修复 (ArgumentOutOfRangeException: capacity)
- **根因分析**：原版 Terraria 1.4 中 `Recipe.requiredTile` 为单个 `int` 字段（徒手制作为 `-1`）。在 [`UIRecipePath.cs`](file:///c:/Users/loris/Documents/Cirno9TerrariaMods/tPlainModLoader/Mods/RecipeBrowser/RecipeBrowser/UIElements/UIRecipePath.cs) 中误调用了 `new HashSet<int>(int capacity)`，传入 `-1` 导致每帧无限抛出 `capacity` 越界异常。
- **优化措施**：显式实例化空集合并对 `requiredTile >= 0` 进行安全 `Add`，彻底消除每帧异常。

### 4.4 全面接入 `TPML.Core.Logging` 新日志体系与设置面板
- **新日志体系**：淘汰旧的 `tpmlLog.txt`，全面对接 [`TPML.Core.Logging.LogManager`](file:///c:/Users/loris/Documents/Cirno9TerrariaMods/tPlainModLoader/tPlainModLoader/TPML.Core/Logging/LogManager.cs) 异步批量落盘体系（`tpml.log` 与历史归档 `tpml_old.log`），提供带精确毫秒时间戳与级别标签的结构化日志；
- **配置化与游戏内设置**：
  1. [`RBProfiler.cs`](file:///c:/Users/loris/Documents/Cirno9TerrariaMods/tPlainModLoader/Mods/RecipeBrowser/RecipeBrowser/Common/RBProfiler.cs) 默认关闭并短路（0 性能开销）；
  2. 新建 [`RecipeBrowserSetting.cs`](file:///c:/Users/loris/Documents/Cirno9TerrariaMods/tPlainModLoader/Mods/RecipeBrowser/RecipeBrowser/RecipeBrowserSetting.cs) 接入 TPML `ModSetting` 框架，支持在游戏内模组设置菜单中自由开关 Profiler 与各项偏好。

### 4.5 掉落物缓存体系自愈与掉落物过滤恢复
- **问题分析**：勾选物品图鉴中的“仅显示掉落物”时，列表显示为空。原因是原版 tModLoader 的掉落物收集逻辑在 TPML 环境下未触发。
- **优化措施**：
  1. 编写独立模块 [`LootCacheManager.cs`](file:///c:/Users/loris/Documents/Cirno9TerrariaMods/tPlainModLoader/Mods/RecipeBrowser/RecipeBrowser/LootCacheManager.cs)，通过 `Main.ItemDropsDB.GetRulesForNPCID` 安全遍历原版掉落规则，解析出 723 个掉落物项并填充 `LootCache.instance.lootInfos` 与 `ItemCatalogueUI.isLoot`；
  2. 在 `ItemCatalogueUI` 与 `BestiaryUI` 中增加空缓存自愈检测，确保打开 UI 时自动初始化。

### 4.6 角标与制作站贴图渲染安全重构
- **问题分析**：
  1. `UIItemSlot` / `UICatalogueSlots` 访问未加载的导线角标纹理导致 `ReLogic.Content.AssetLoadException` 崩溃；
  2. `Utilities.GenerateTileTexture` 原先在 UI 绘制期间通过 GPU `RenderTarget2D` + `spriteBatch.Begin()` 嵌套合成，打乱渲染管线导致 `Cannot call Present when a render target is active` 崩溃。
- **优化措施**：
  1. 导线贴图访问全面增加 `IsLoaded` 守护与 `try...catch` 防护；
  2. 将制作站贴图生成重构为纯 CPU 内存像素拷贝（`GetData` / `SetData`），彻底脱离 GPU 渲染目标管线，消灭帧末 Present 崩溃。

### 4.7 制作树节点本地化与 ItemHoverTag 全面渲染
- **问题分析**：制作面板中的缺少材料、已拥有材料等节点此前直接输出了未翻译英文及原始 ID 列表（如 `Missing: 9/619/620/... x1`）。
- **优化措施**：
  1. 重构 [`CraftPath.cs`](file:///c:/Users/loris/Documents/Cirno9TerrariaMods/tPlainModLoader/Mods/RecipeBrowser/RecipeBrowser/CraftPath.cs) 中全部节点（`UnfulfilledNode`, `HaveItemNode`, `HaveItemsNode`, `BuyItemNode`, `LootItemNode`, `MineItemNode`, `BugNetItemNode`, `JourneyDuplicateItemNode`）的 `ToUITextString()` 渲染逻辑；
  2. 统一调用 [`ItemHoverFixTagHandler`](file:///c:/Users/loris/Documents/Cirno9TerrariaMods/tPlainModLoader/Mods/RecipeBrowser/RecipeBrowser/TagHandlers/ItemHoverFixTagHandler.cs) 与 [`NPCTagHandler`](file:///c:/Users/loris/Documents/Cirno9TerrariaMods/tPlainModLoader/Mods/RecipeBrowser/RecipeBrowser/TagHandlers/NPCTagHandler.cs)，配合 [`zh-Hans.hjson`](file:///c:/Users/loris/Documents/Cirno9TerrariaMods/tPlainModLoader/Mods/RecipeBrowser/RecipeBrowser/Resources/Localization/zh-Hans.hjson)，将所有条目转换为带悬停 Tooltip 的图文标签（如 `缺少: [凝胶图标]`, `缺少: [木材图标 (任何木材)]`）。

### 4.8 界面尺寸放大（640x520）与原生几何居中（HAlign/VAlign 0.5f）
- **优化措施**：
  1. 默认窗口尺寸由 `475 × 350` 放大至 **`640 × 520`**；
  2. 主面板采用 UI 原生几何居中（`HAlign = 0.5f, VAlign = 0.5f, Left = 0, Top = 0`），由 Terraria UI 布局引擎自动确保在 1080p、2K、4K 及带鱼屏等各种分辨率下绝对正中央弹出；
  3. [`UIDragableElement.cs`](file:///c:/Users/loris/Documents/Cirno9TerrariaMods/tPlainModLoader/Mods/RecipeBrowser/RecipeBrowser/UIElements/UIDragableElement.cs) 在拖拽/缩放结束时自动保存相对位置与尺寸至本地配置。

---

## 5. GABS 自动化端到端实机回归测试

本模组开发了专属的 GABP 自动化实机测试套件，在原版游戏中完成了 10 个阶段的全链路自动化回归断言：

```powershell
uv run python Scripts/test_tpml_recipe_browser.py
```

### 5.1 测试用例与阶段覆盖

| 测试阶段 | 测试用例与断言内容 | 实机验证结果 |
| :--- | :--- | :---: |
| **Phase 1** | GABP 协议握手、9 个 RecipeBrowser 专属自动化工具（状态探测、面板开闭、Tab 切换、配方过滤、查询槽位、合成树分解、配方收藏、图鉴过滤与状态获取）就绪性检测 | **PASS (100%)** |
| **Phase 2** | 秒级载入 `Test` 角色与 `Test` 测试世界，动态启用只读存档保护机制（拦截一切写盘操作） | **PASS (100%)** |
| **Phase 3** | 全景状态探测（3612 条原版配方已载入），主面板开启与关闭状态机切换 | **PASS (100%)** |
| **Phase 4** | 5 大核心 Tab 页面（Recipes / Craft / Items / Bestiary / Help）无缝切换与激活 | **PASS (100%)** |
| **Phase 5** | 配方关键词中英双语检索（如 `'Night\'s Edge'` 匹配 3 条）、木材查询槽位过滤（匹配 163 条）与重置 | **PASS (100%)** |
| **Phase 6** | 复杂物品递归合成树深度分析：永夜刃（ItemID: 273）成功解析出 5 个节点（包含前置 4 把名剑、制作台与原材料），且全部以 `[itemhover]` 图文标签渲染 | **PASS (100%)** |
| **Phase 7** | 配方收藏/取消收藏状态流转，收藏夹悬浮面板开启，Sidecar 伴随数据链路无缝同步 | **PASS (100%)** |
| **Phase 7.5** | 物品图鉴掉落物与可制作过滤专项断言：断言仅显示掉落物匹配 723 条（>0 且与提取缓存条目一致）、仅显示可制作匹配 3215 条、重置恢复全量 6167 条 | **PASS (100%)** |
| **Phase 8** | 物品图鉴高频选物（永夜刃、泰拉刃、铁阔剑、火把）并秒切制作树 Tab 端到端渲染与高频稳定性断言 | **PASS (100%)** |
| **Phase 8.5** | 实机读取 `tpml.log` 运行日志，断言 0 绘制异常、0 裁剪异常、0 Present 崩溃且实时记录完整 | **PASS (100%)** |
| **Phase 9** | 安全退出测试世界，确认玩家世界存档 100% 原始未被写盘污染 | **PASS (100%)** |

---

## 6. 原版差距补完（2025-06 全量对齐）

基于与原版 `RecipeBrowser v0.12`（`TModSource/RecipeBrowser/v0.12/Source`）的逐层对比（4 个子代理 + 人工抽查），完成以下补完。构建验证：`RecipeBrowser.csproj -c Release` **0 警告 0 错误**，自动部署至游戏 Mods 目录。

### 6.1 批次 1：数据正确性（P0）

| 项 | 改动文件 | 内容 |
| :--- | :--- | :--- |
| CraftPath 库存感知 | `CraftPath.cs` | 恢复原版五分支决策（JourneyDuplicate / HaveItem / HaveItem+Unfulfilled / HaveItems 分摊 / Unfulfilled）；恢复 `ConsumeResources`/`UnConsumeResources` 消耗回退体系；`Push`/`Pop` 完整维护树结构与库存视图 |
| 环检测语义修复 | `CraftPath.cs` | `CheckParentsForRecipeLoopViaIngredients` 只移除祖先 `createItem.type`（不再误删可行原料路径） |
| 空结果语义 | `RecipePath.cs` | 恢复原版"无解返回空列表"（删除移植版塞入根路径的兜底） |
| Mod 过滤主开关 | `RecipeCatalogueUI.cs` | `PassRecipeFilters` 补 `ModIndex != 0` 判定（结果物品/原料须属于选中模组，Terraria 分支按 `type < ItemID.Count`） |
| 图鉴 Mod 过滤框架 | `BestiaryUI.cs` | `PassNPCFilters` 补 ModIndex 过滤（TPML 无模组 NPC，非 Terraria 时为空） |

### 6.2 批次 2：核心功能（P0-P1）

| 项 | 改动文件 | 内容 |
| :--- | :--- | :--- |
| 物品掉落查看器 | `ItemCatalogueUI.cs`、`SharedUI.cs`、`LootCacheManager.cs` | 恢复掉落面板：`LootCacheManager` 新增"物品→掉率"缓存（遍历 `GetRulesForNPCID` 聚合，替代 tML 的 `GetRulesForItemID`）；逐条 `UIBestiaryInfoItemLine`（条件不满足涂红）+ ExpectedValue 金币合计；`SharedUI.ShouldShowItemDrop` |
| 分类体系补全 | `SharedUI.cs`、`Utilities.cs` | 恢复 6 全局排序（含 CreativeSort，用原版 `ContentSamples.ItemCreativeSortingId`）；完整分类树（Weapons 8 子类 / Tools 3 / Armor Sets / Armor 3 / Tiles 11 / Walls / Accessories-Wings / Ammo / Potions 4 / Expert / Pets / Mounts / Hooks / Dyes / Consumables / Grab Bags / Fishing 4 / Extractinator / Other）+ 专属排序（Damage/PickPower/Wings×3/GrappleRange 等）+ 专属过滤（Ammo/弹药循环、Vanity/ArmorOnly 互斥、Solid/NonSolid 互斥）；恢复 `modCategories`/`modFilters` 消费循环；`Utilities` 补 CPU 版 `ResizeImage`/`StackResizeImage` |
| 查询历史导航 | `UIQuerySlots.cs`、`RecipeCatalogueUI.cs` | 恢复 `history`/`AddToHistory`/`GoBackInHistory`/`GoForwardInHistory` + 面板历史前进/后退按钮（复用 `HistoryBack`/`HistoryForward` 贴图与本地化键） |
| Armor Sets 接线 | `ArmorSetFeatureHelper.cs`、`ItemCatalogueUI.cs` | 名称前缀匹配 + 两件套组合（三件套/身腿/头身/头腿去重）；`armorSetSlots` + `AppendSpecialUI`（4 复选框控制面板）；`ItemCatalogueUI.Update` 在 Armor Sets 分类填充套装槽位；`ItemGridSort` 按套装总防御排序 |

### 6.3 批次 3：标签处理器（P1）

| 项 | 改动文件 | 内容 |
| :--- | :--- | :--- |
| ItemHoverFix 双语法 | `TagHandlers/ItemHoverFixTagHandler.cs` | 原版 options 语法（`d`/`o`/`c`/`t`/`s`/`x`/`p`）+ 移植版冒号语法向后兼容；`GenerateTag` 输出原版格式；稀有度着色、完整 tooltip 悬停、`type<=0` 回退文本 |
| ImageTagHandler | `TagHandlers/ImageTagHandler.cs` | 恢复 `t`（tooltip）/`s`（scale）/`v`（vOffset）选项、资源容错回退纯文本、悬停提示 |
| NPCTagHandler | `TagHandlers/NPCTagHandler.cs` | 恢复 `head` 选项 + 身体贴图动画帧、范围校验、NPC 染色、`npcArrow` 箭头追踪与点击 Ping |
| LinkTagHandler | `TagHandlers/LinkTagHandler.cs` | `GenerateTag` 参数顺序对齐原版（url, text）、悬停显示 url |

### 6.4 批次 4：UI 交互对齐（P1）

| 项 | 改动文件 | 内容 |
| :--- | :--- | :--- |
| UIRecipeSlot 交互 | `UIElements/UIRecipeSlots.cs` | 右键→打开制作页并设配方、双击→清过滤器+查询、左键补 `queryLootItem`/`playerInventory`/`focusRecipe`；悬停上报 `hoveredIndex`、收藏键光标覆盖、新发现背景；收藏按收藏顺序排序 |
| UIMockRecipeSlot | `UIElements/UIRecipeSlots.cs` | `ableToCraftBackgroundTexture` 懒加载；左键按 craftPaths 跳制作页/Goto 定位；右键关闭浏览器；收藏键光标 |
| UICraftButton | `UIElements/UIRecipeSlots.cs` | 纹理帧序修复（悬停且可合成→高亮帧）+ ✓/X 状态字 + 悬停 MenuTick |
| 查询槽 | `UIElements/UIQuerySlots.cs` | `ReplaceWithFake` 归还物品本体（保留词缀）；恢复 `CanonicalItemType` 配方组映射表 |
| 配方信息三态 | `UIElements/UIRecipeInfoElements.cs` | `UIRecipeInfoRightAligned` 恢复 ✓/X/? 三态 + Missing/Unseen 悬停 + 水/蜜/岩浆条件文本 |

### 6.5 批次 5：细节打磨（P2）

| 项 | 改动文件 | 内容 |
| :--- | :--- | :--- |
| Mod 图标 | `RecipeBrowserUI.cs` | `UpdateModHoverImage`：读取模组 `icon.png` 作过滤按钮图标（`mod.GetFileBytes`） |
| Mod 下拉截断 | `UIElements/ModFilterDropdown.cs` | 超长模组名二分截断省略号 + 悬停全文 |
| 收藏面板 | `RecipeBrowserUI.cs`、`UIDragablePanel.cs` | 关闭按钮热键提示（当前绑定键）；收藏面板位置拖拽持久化 |
| 主面板钳制 | `RecipeBrowserUI.cs` | 面板不拖出屏幕 |
| 搜索防呆 | `RecipeCatalogueUI.cs`、`ItemCatalogueUI.cs` | 结果为空时回退删除末字符 |
| Unload 清理 | `RecipeBrowserMod.cs` | `RBTextures.Clear()`、`UIItemSlot.hoveredItem`、`availableRecipesCache`、`tileTextures` 等静态清理 |
| 配置默认值 | `RecipeBrowserClientConfig.cs` | `OnlyShowFavoritedWhileInInventory` 默认改回 `true`（对齐原版） |
| 箭头追踪 | `RecipeBrowserUI.cs`、`RecipeBrowserPatchMain.cs`、`NPCTagHandler.cs` | `HandleArrow` 实现并接入 Arrow 层 |
| 拾取发现 | `RecipeBrowserPlayer.cs` | `OnPickup` + 进世界背包扫描 → `ItemReceived`（对齐 GlobalItem.OnPickup 语义） |
| Call API | `RecipeBrowserMod.cs`、`SharedUI.cs` | `AddItemCategory`/`AddItemFilter` 跨模组注册入口 + `SetupAgain()` 重建 |
| Tool 虚方法 | `ToolsAndState.cs` | 补 `ClientInitialize`/`PostSetupContent`/`Toggled`/`DrawUpdateToggle` |

### 6.6 框架边界（TPML 无对应能力，记录为已知限制）

| 原版能力 | 处理 |
| :--- | :--- |
| 多人网络同步（收藏共享/宝箱请求/他人收藏面板） | 用户确认跳过；预留字段与注释（`ModNetPacket` 结构已调研可用） |
| ItemChecklist 集成（未获得/新物品筛选） | TPML 生态无此模组，省略；`foundItems`/`newestItem` 保留为预留字段 |
| 模组 NPC（ModNPC/NPCLoader） | TPML 框架无，LootCache 与图鉴保持原版 NPC 边界 |
| `Recipe.Disabled`（禁用配方） | TPML 原版 Recipe 无此字段，Disabled 过滤无数据源 |
| Boss Summons 分类 / ProgressionOrder | 依赖 tML `SortingPriorityBossSpawns`，省略 |
| Master 分类 | 依赖 tML `Item.master`，省略 |
| Grab Bags 精确谓词 / ExpectedValue 排序 | 依赖 tML `GetRulesForItemID`，用 BossBag/钓鱼箱近似 |
| 多工作台配方（requiredTile 集合） | TPML `RecipeLoader` 模型为单 int，按第一个台座处理 |
| `DrawWindowsIMEPanel` / `LockVanillaMouseScroll` / `OpenToURL` / `CursorTextures` / `recFastScroll` | tML 扩展 API，TPML 原版无，已适配或省略 |
| `Player.setBonus`（套装验证） | tML 扩展，Armor Sets 用名称前缀匹配 + 两件套近似 |
| `PlayerDisconnect` / `ModifyDrawInfo` | TPML ModPlayer 无对应钩子，省略 |

