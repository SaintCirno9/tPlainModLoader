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

---

## 5. GABS 自动化端到端实机回归测试

本模组开发了专属的 GABP 自动化实机测试套件，在原版游戏中完成了 9 个 Phase 的全链路自动化回归断言：

```powershell
uv run python Scripts/test_tpml_recipe_browser.py
```

### 5.1 测试用例与阶段覆盖

| 测试阶段 | 测试用例与断言内容 | 实机验证结果 |
| :--- | :--- | :---: |
| **Phase 1** | GABP 协议握手、7 个 RecipeBrowser 专属自动化工具（状态探测、面板开闭、Tab 切换、配方过滤、查询槽位、合成树分解、配方收藏）就绪性检测 | **PASS (100%)** |
| **Phase 2** | 秒级载入 `Test` 角色与 `Test` 测试世界，动态启用只读存档保护机制（拦截一切写盘操作） | **PASS (100%)** |
| **Phase 3** | 全景状态探测（3610 条原版配方已载入），主面板开启与关闭状态机切换 | **PASS (100%)** |
| **Phase 4** | 5 大核心 Tab 页面（Recipes / Craft / Items / Bestiary / Help）无缝切换与激活 | **PASS (100%)** |
| **Phase 5** | 配方关键词中英双语检索（如 `'Night\'s Edge'` 匹配 3 条）、木材查询槽位过滤（匹配 163 条）与重置 | **PASS (100%)** |
| **Phase 6** | 复杂物品递归合成树深度分析：永夜刃（ItemID: 273）成功递归解析出前置 4 把名剑（火山、村正大刀、草剑、光之驱逐）与制作台依赖节点 | **PASS (100%)** |
| **Phase 7** | 配方收藏/取消收藏状态流转，收藏夹悬浮面板开启，Sidecar 伴随数据链路无缝同步 | **PASS (100%)** |
| **Phase 8** | 物品图鉴高频选物（天顶剑、永夜刃、泰拉刃、铁阔剑）并秒切制作树 Tab 端到端渲染与稳定性断言 | **PASS (100%)** |
| **Phase 8.5** | 实机读取 `tpml.log` 运行日志，断言 0 绘制异常、0 裁剪异常、0 Present 崩溃且实时记录完整 | **PASS (100%)** |
| **Phase 9** | 安全退出测试世界，确认玩家世界存档 100% 原始未被写盘污染 | **PASS (100%)** |

