# Recipe Browser 模组筛选按钮左键交互与弹窗修复 Walkthrough

## 1. 修复背景与问题定位

用户反馈在移植版 Recipe Browser 中，右上角 Mod 过滤按钮**右键与中键功能正常**，但**左键点击无效且会导致界面卡住、无法弹出模组筛选列表**。

### 根因分析
1. **全屏透明遮罩拦截（导致界面假死）**：旧版 `BlockInput` 在最顶层 `UIModState` 上创建了 `100%` 宽高的全屏透明 `blockInputElement`，左键点击后全屏输入均被拦截，主面板所有组件无法接收鼠标事件；
2. **绘制顺序倒置（导致弹窗被覆盖遮挡）**：`Tool.UIDraw` 在每帧绘制前调用了 `uistate.ReverseChildren()`，导致原本作为最新子元素加入的弹窗面板被最先绘制，随后 `mainPanel` 在其上方重绘，将筛选面板完全盖住；
3. **架构与样式偏离原版**：原版 Recipe Browser 将遮罩与下拉面板作为 `mainPanel` 内部子元素挂载（仅遮挡 Top 20px 以下的内容区，保留顶部标签栏可操作），且右侧滑出 300px 暗红侧边栏。

---

## 2. 变更实施内容

### [MODIFY] [ToolsAndState.cs](file:///c:/Users/loris/Documents/Cirno9TerrariaMods/tPlainModLoader/Mods/RecipeBrowser/RecipeBrowser/ToolsAndState.cs)
- 精简 `UIModState`，移除非法的全局全屏 `blockInputElement`、`IsInputBlocked`、`BlockInput`、`UnblockInput` 逻辑，还原为纯净的 UI 状态机基类。

### [MODIFY] [ModFilterDropdown.cs](file:///c:/Users/loris/Documents/Cirno9TerrariaMods/tPlainModLoader/Mods/RecipeBrowser/RecipeBrowser/UIElements/ModFilterDropdown.cs)
- **右侧滑出面板布局**：宽度 `300px`，高度 `Height.Set(-50f, 1f)`，`Top.Set(20f, 0f)`，`Left.Set(-300f, 1f)`，暗红主题背景（`Color.DarkRed`）；
- **内部容器与滚动条**：嵌入 `UIPanel`（`BackgroundColor = Color(200, 50, 50, 255)`）与 `UIList`、`InvisibleFixedUIScrollbar`，顶部加入 2px 魔术像素对齐遮罩条；
- **行项组件（`ModFilterDropdownRow`）**：
  - 高度 `30px`，支持鼠标悬停高亮与移出恢复；
  - 悬停时动态设置 `RecipeBrowserUI.modHoverIndex`，右上角 Mod 按钮实时预览对应模组的 `icon.png`；
  - 采用二分查找自适应截断超长模组名称（`ComputeTruncationOnce`），并在截断悬停时显示完整 `UICommon.TooltipMouseText`；
  - 支持 `SelectIndex(int index)` 外部联动高亮。

### [MODIFY] [RecipeBrowserUI.cs](file:///c:/Users/loris/Documents/Cirno9TerrariaMods/tPlainModLoader/Mods/RecipeBrowser/RecipeBrowser/RecipeBrowserUI.cs)
- **精准区域遮罩（`BlockInput` / `UnblockInput`）**：
  - `blockInput = new BlockInputElement(mainPanel, 20)`：仅遮挡 `mainPanel` 内容区域；
  - 点击主面板任意空白处或再次点击 Mod 按钮平滑退出；
  - 退出时自动重置 `modHoverIndex = -1` 并恢复 Mod 按钮图标；
- **Mod 按钮左键/右键/中键联动**：
  - 左键：单例创建/复用 `ModFilterDropdown`，实现打开/关闭切换及选中关闭；
  - 右键：循环切换至上一个模组，并同步更新 `ModFilterDropdown.SelectIndex(ModIndex)`；
  - 中键：重置为全部（Terraria），并同步更新 `ModFilterDropdown.SelectIndex(ModIndex)`；
- **模组显示名与图标加载**：
  - `GetDisplayName(int index)` 优先获取 `Mod.DisplayName`；
  - `UpdateModHoverImage()` 支持 80x80 图标流式加载与缓存保护。

---

## 3. 构建与审查验证

### 构建测试
- 执行全量多核极速构建：
  ```pwsh
  dotnet build tPlainModLoader/tPlainModLoader/tPlainModLoader.sln -c Release -m /graph
  ```
- **构建结果**：0 警告，0 错误，19 个工程全部编译成功，RecipeBrowser.dll 自动部署至游戏目录。

### 代码审查要点
- [x] 二分文本截断算法区间严格收敛，已防护 `width <= 0` 及空文本边界；
- [x] `UpdateModHoverImage` 具备 `num == modIndexPrevious` 缓存检查，避免每帧流式读取纹理造成 GC 压力；
- [x] `BlockInput` 仅拦截内容区域，不破坏主面板关闭按钮与顶栏 Tab 切换；
- [x] 切换 Tab 时 `TabController` 会将打开的下拉面板重新置顶，层级稳定无遮挡。
