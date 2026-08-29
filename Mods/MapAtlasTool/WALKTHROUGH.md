# MapAtlasTool 拆分实施记录 (WALKTHROUGH)

> 任务: 将 OptimizeAndTool"重要结构显示"功能抽成独立模组 MapAtlasTool（地图图鉴工具）
> 日期: 2026-08-29 | 作者: SaintCirno9 | 状态: 编译通过并部署, 实机冒烟未执行（见下）

## 实施结果

### 新增 MapAtlasTool (`tPlainModLoader/Mods/MapAtlasTool/`)

| 文件 | 说明 |
|---|---|
| `MapAtlasTool/MapAtlasTool.csproj` | net472, 仅引用 TPML.Core/tContentPatch/TPML.Content（无 Harmony/CommandHelp）, DeployToGameDir 热部署 |
| `info.json` / `loadConfig.json` / `ico.png` | 显示名"地图图鉴工具", key `StaticTile.MapAtlasTool`, version 1.0.0 |
| `AtlasValSet.cs` | 25 个显示开关（19 结构 + 5 宝箱雷达 + 总开关）+ SetAllStructureMarkers + 面板抽屉树构建 |
| `SettingUIAtlas.cs` | ModSetting 持久化, **全量持久化 25 开关 + 面板展开状态**（旧 OAT 仅持久化 7 个）, HasUI=false（面板即唯一 UI） |
| `Content/StructureMarker.cs` | 原 OAT 1349 行整体迁移 + 搜索高亮/双击飞视图/NPC 表化改造 |
| `Content/ChestItemIndex.cs` | 箱子物品索引（后台构建）+ 统一箱子搜索（物品名拼音/ID + 自定义箱名, 实时校验防陈旧）+ N/M 统计 |
| `Content/UI/MapAtlasPanel.cs` | 全屏大地图侧边栏面板: 搜索框(IME/防抖 300ms/右键清空) + 统计行 + 分组结果列表 + 开关抽屉树 + 快捷按钮; 自有 UserInterface 在 DrawMapPostfix 驱动; UpdatePostfix 吞面板区域地图输入 |
| `Content/UI/` + `Utils/` 8 个基建文件 | 自 OAT 复制: GetSetReset、BindUIAVal、UIBuild(裁剪 get1/get5)、UIDrawer、UIItemMouseText、UIItemSwitchBind、UIItemButton、UITextButton |
| 已加入 `tPlainModLoader.sln` | GUID {0957943E-47A8-4049-A05A-8CBFF989E0E9} |

### 核心功能行为

- **搜索权威**: 命中图钉无视分类开关高亮（白色脉动光圈、置顶绘制），未命中调暗 40%（仍受分类开关过滤）；三种地图形态统一。
- **统一搜索**: 结构名 / 受困 NPC 名（不进列表仅高亮）/ 箱子自定义名 / 箱内物品名；匹配中文原名 + 全拼 + 拼音首字母 + 物品 ID（`TPML.Core.PinyinHelper.Matches`）。
- **结果列表**: 按"结构/遗迹 → 宝箱"分组，双击条目或双击地图图钉经原版 `Main.PanTargetMapFullscreen` 动画飞至目标（不传送玩家）；列表分别上限 100/200 条（超出提示，地图高亮仍全量）。
- **箱子索引**: 进世界随结构扫描后台构建（itemId → chestIndex），渲染/悬停实时校验箱子当前内容防陈旧；面板显示"已索引 N/M"（多人纯客户端仅同步过的箱子有物品数据）。

### OptimizeAndTool 侧删除

- 删 `Content/Cheat/QoL/StructureMarker.cs`、`docs/P4_P6_遗留改造方案.md`（docs 空目录一并移除）；
- `QoLValSet.cs`: 25 个 mark* 字段、SetAllStructureMarkers、25 条聊天命令、四个设置抽屉段；
- `SettingUI.cs`: Data 的 7 个 Mark* 字段及 Load/GetSaveData/SetDefault/OnValUpdate 对应行；
- `Function.cs`: GetCO/GetUI 两处 QoLValSet 聚合行;
- `loadConfig.json` version 1.0.2 → 1.0.3。
- 全仓 rg 确认 OAT 内无 StructureMarker/mark* 残留引用。

## 编译验证

- `dotnet build tPlainModLoader/tPlainModLoader/tPlainModLoader.sln -c Release -m -graph` **21 个工程全部成功**，MapAtlasTool 与 OptimizeAndTool 均自动热部署至 `C:\Games\Steam\...\Terraria\tPlainModLoader\Mods\`。
- 迭代修复的编译错误（均为迁移期问题）: 漏复制 OAT 本地控件 BindUIAVal/UITextButton（并非框架件）、ModSetting/quickBuild using 缺失、UITextBox 与原版同名二义（加 using 别名）、UIState vs UIElement（SetState 签名）、NPCID 常量为 short 需显式 `new int[]`。

## 偏差记录

- 计划中"复制 6 个基建文件"实际为 **8 个**（BindUIAVal.cs、UITextButton.cs 是 OAT 本地件而非框架件，调研阶段误判，编译期发现补齐）。
- 结果列表设了 100/200 条显示上限（地图高亮不限量）——滚动列表 UIElement 数量约束的必要折中，符合"高亮全量、列表可浏览"意图。

## 实机冒烟: 未执行

GABS 在本机不可用（CLI 未安装、常见安装目录无、MCP 工具未挂载），无法按计划执行实机回归。编译验证已通过；建议在 GABS 环境可用后按以下清单冒烟（Test 人物/世界，TPMLBridge 拦截存盘）:

1. 进世界 → 聊天提示"扫描完成，共索引 N 处世界结构与宝箱"；
2. M 打开全屏地图 → 左上角折叠按钮出现 → 点击展开面板（标题"地图图鉴"）；
3. 搜索"宝箱"/"bx"/"图鉴中任一结构名"/"3874"（物品 ID）→ 命中图钉白圈脉动、未命中调暗、结果列表分组正确、统计行显示"已索引 N/M"；
4. 双击列表条目 → 地图动画平移至目标；双击地图命中图钉 → 同样飞至；
5. 面板内拖动标题栏/拖右下角调整大小、开关抽屉切换、全部开启/关闭、重新扫描按钮；
6. 悬停命中箱图钉 → 战利品清单 tooltip；关闭地图重开 → 面板状态与开关持久化恢复；
7. OptimizeAndTool 设置界面确认"地图标记关键结构与宝箱"抽屉已消失、其余功能正常，无图钉双画。
