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
4. **建筑蓝图管理器与原地重命名 (Blueprint Library)**：
   - 采用标准紧凑字号 `MouseText (0.85f)`，彻底移除原版不支持的 emoji 字符以防乱码；
   - 提供原地重命名编辑模式，内嵌支持中文 IME 输入法的 `UITextBox`，支持平滑保存与取消。
5. **Loader 核心级文本输入全局静默**：
   - 将文本输入防冲突逻辑下沉至 Loader 底层 `tContentPatch.Input.ModKeybind`；
   - 在任何输入框获得焦点或打字时，系统全局拦截并静默所有 `ModKeybind`，彻底避免打字误触发巨大背包、吸管工具等快捷键。
6. **背包状态联动 QoL**：
   - 监听背包开启/关闭状态切换，当玩家按 `Esc`/`Tab` 操作背包时自动退出魔杖模式，重置所有魔杖模式、清空选区与操作队列。

---

## 2. 核心架构与模块划分

```
WandsTool/
├── WandsTool.csproj                    # SDK 风格 MSBuild 工程文件（含 DeployToGameDir）
├── feces.cs                            # 模组主生命周期与 UI 钩子（继承 PatchMain）
├── ListenInput.cs                      # 本地按键输入监听
├── Content/
│   ├── Wands/
│   │   ├── Wands.cs                    # 建造魔杖核心轮询、光标跟随提示渲染
│   │   └── WandAction.cs               # 魔杖物块/墙壁放置与队列执行动作
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
   - 18 个工程全量构建通过，耗时 ~4.1s，无任何回归问题。
