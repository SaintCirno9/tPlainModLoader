# MapAtlasTool 实施计划

> 将 OptimizeAndTool 的"重要结构显示"功能抽成独立模组 **MapAtlasTool（地图图鉴工具）**。
> 作者: SaintCirno9 | 日期: 2026-08-29 | 状态: 实施中

## 目标

- 全量平移现有 24 项结构/宝箱标记（19 结构 + 5 宝箱雷达 + 受困 NPC 动态标记）；
- 新增全屏大地图侧边栏面板：显示设置开关树 + 统一搜索（结构名 / 受困 NPC 名 / 箱子自定义名 / 箱内物品名），支持中文 + 全拼 + 拼音首字母 + 数字 ID；
- 搜索命中图钉白色脉动光圈置顶，未命中调暗约 40%，三种地图形态统一；搜索权威，无视分类开关；
- 结果列表按"结构 / NPC / 箱子"分组、限高滚动、显示总数，悬停显示详情，双击列表项或图钉使地图视图飞到目标（原版 `Main.PanTargetMapFullscreen` 动画，不传送玩家）；
- 面板显示"已索引 N/M 个箱子"（多人纯客户端只有打开过的箱子有物品数据）；
- 箱子物品索引进世界后台构建，渲染/悬停时实时校验箱子内容防陈旧；
- OptimizeAndTool 侧旧实现彻底删除（避免双画）。

## 已核实的关键事实

- 迁移核心 `StructureMarker.cs` 1349 行，仅依赖框架 `PatchMain`（LoadInstance 反射自动加载，无需注册）、TPML.Core 日志；不依赖 Fusion/存储系统。
- 复制链 6 文件约 350 行：`Utils/GetSetReset.cs`、`Content/UI/UIItemMouseText.cs`、`UIItemSwitchBind.cs`、`UIItemButton.cs`、`Content/UI/UIDrawer.cs`、`Utils/quickBuild/UIBuild.cs`（裁掉 get1/get5 避免拖数值输入控件链）。UISwitch/UIStackPanel/UIScrollViewer2/UITextBox/UIItem/BindUIAVal/UITextButton 均为 tContentPatch 框架公共件，直接 ProjectReference 引用。
- 搜索基础设施现成：框架 `tContentPatch.Content.UI.UITextBox`（IME/防抖，309 行）、`TPML.Core.Pinyin.PinyinHelper.Matches / MatchesPinyin`（Trie + 3.5 万行嵌入式词库，零外部依赖）；匹配范本 `OptimizeAndTool/.../Storage/Core/BagCategoryHelper.cs` 的 `MatchesSearch`。
- 单机世界加载后 `Main.chest[8000]` 全部物品常驻内存（`WorldFile.LoadChests` 直接 `netDefaults` 实例化）；多人纯客户端仅打开过的箱子同步物品内容。
- 飞视图用原版 `Main.PanTargetMapFullscreen` / `Main.PanTargetMapFullscreenEnd`（Main.cs:925/927）。
- 原版 `Main.DrawMap` 一个方法覆盖全屏大地图 / 小地图(mapStyle 1) / 悬浮地图(mapStyle 2)；TPML 框架已有 `PatchMain.DrawMapPostfix` Harmony Postfix（`tContentPatch/ModPatch/Patch_Main.cs:202`）。
- OAT 旧功能聚合点：`Content/Function.cs` L82（GetCO）与 L171（GetUI）；`SettingUI.Data` 仅持久化 7 个 Mark* 字段（其余 18 个为会话级开关）——新 mod 全量持久化（行为增强）。

## 任务清单

- [x] 1. 工程骨架：三层目录 + csproj + info.json + loadConfig.json + ico.png + 加入 sln
- [x] 2. 复制 UI 基建文件并改 namespace（实际 8 个: 另含 OAT 本地件 BindUIAVal/UITextButton）
- [x] 3. 迁移 StructureMarker + AtlasValSet + SettingUIAtlas
- [x] 4. 大地图侧边栏面板 MapAtlasPanel（搜索框/统计行/结果列表/开关抽屉/按钮行，自有 UserInterface 驱动）
- [x] 5. ChestItemIndex 搜索索引 + 搜索匹配 + 高亮渲染 + 飞视图
- [x] 6. OptimizeAndTool 侧删除（StructureMarker.cs、QoLValSet 25 开关、SettingUI 7 字段、Function.cs 两行、docs/P4_P6 文档、loadConfig version bump）
- [x] 7. 全量构建 `dotnet build tPlainModLoader/tPlainModLoader/tPlainModLoader.sln -c Release -m -graph`（21 工程全部通过并热部署）
- [ ] 8. GABS 实机冒烟（未执行: GABS 环境不可用; 手动清单见 WALKTHROUGH.md）
- [x] 9. WALKTHROUGH.md 记录

## 工程约定

- 目录：`tPlainModLoader/Mods/MapAtlasTool/MapAtlasTool/MapAtlasTool/`（三层，与 OptimizeAndTool 同构）；
- csproj：net472、无 Lib.Harmony（无自身 patch）、无 CommandHelp（无聊天命令）；ProjectReference 仅 TPML.Core / tContentPatch / TPML.Content（Private=false）；保留 `DeployToGameDir` 热部署 target；
- loadConfig key：`StaticTile.MapAtlasTool`；
- 设置持久化：框架 ModSetting（setting.json），面板即唯一 UI，无独立设置界面、无聊天命令；
- 不主动提交 git。

## 风险与对策

- 全屏地图态原版 UI 层不活动 → 在 `DrawMapPostfix` 中用自有 `UserInterface` 实例驱动 Update/Draw（范本 UniversalBagWindow）；
- 面板与地图拖拽/底部关闭按钮事件冲突 → 面板置屏幕左侧避开底部关闭按钮 + 面板矩形内吞掉鼠标事件；
- 面板内部控件（UIDrawer/UIItemSwitchBind 等）依赖框架 UserInterface 体系（UIElement.Update 等），需确认在自定义 UserInterface 下状态正常。
