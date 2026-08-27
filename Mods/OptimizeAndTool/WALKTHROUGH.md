# OptimizeAndTool - 全量宝箱雷达与结构标记升级 (实施审查与修复记录)

> **模组名称**：OptimizeAndTool
> **适用版本**：Terraria 1.4.4+ / tPlainModLoader (`net472`)
> **作者**：SaintCirno9
> **方案来源**：`implementation_plan.md` & `docs/P4_P6_遗留改造方案.md`

---

## 1. 方案概述

将 `StructureMarker` 与 `QoLValSet` 的地图结构探测扩展为：

1. **全量宝箱实时战利品雷达** —— 索引全部 `Main.chest`（Containers / Containers2 / FakeContainers），`ChestIndex` 直连实时读取 40 槽物品，悬停浮窗显示名称/坐标/前 7 种物品/空箱提示；
2. **结构与微群落大幅扩展** —— 祭坛、暗影珠/猩红之心、神庙门、水之书、炸药、蘑菇地/蚁穴/苔藓/陨石聚类、受困与特殊 NPC 实时追踪；
3. **视口裁剪加速** —— 全屏大地图/小地图/覆盖地图按视野剔除图钉；
4. **后台线程规范与状态快照 (P4)** —— 主线程队列消费、标量参数快照隔离、`ScopedTimer` 耗时剖析；
5. **宝箱全量样式表与纠错 (P6)** —— 52+38 种宝箱精准表驱动与 13 处错误全量修复。

---

## 2. 审查与实施结论（对照方案逐项核实）

### 2.1 与原版帧数据核对结果（本地 GameSource 反编译源码）

| 检测项 | 实现判定 | 原版依据 | 结论 |
|---|---|---|---|
| 恶魔/猩红祭坛 | `frameX % 54 == 0` + `frameX >= 54` | `WorldGen.Place3x2(..., 26, style)`（3 宽 tile，变体步长 54）；`TileLightScanner` 同用 `frameX >= 54` 区分猩红 | ✅ 正确 |
| 暗影珠/猩红之心 | `frameX % 36 == 0` + `frameX >= 36` | 2 宽 tile 步长 36；`TileLightScanner` 同用 `frameX >= 36` | ✅ 正确 |
| 神庙大门 | `ClosedDoor && frameY / 54 == 11 && frameX % 36 == 0` | `WorldGen.PlaceTile(..., 10, ..., 11)`（神庙生成段，wall 87 / type 226 上下文）；`IsLockedDoor` 的 `frameY ∈ [594,646]` | ✅ 正确（已去重） |
| 地牢《水之书》 | `Books && frameX == 90` | `WorldGen` 掉落 `case 50: frameX == 90 → item 165`；`DungeonUtils.GenerateDungeonBook` 写入 `frameX = 90` | ✅ 已修复 |

### 2.2 全量问题处置状态（P1 ~ P6 全闭环）

| # | 问题项 | 处置方式 | 状态 |
|---|---|---|---|
| P1 | 水之书样式号错误（`frameX / 18 == 2` → `frameX == 90`） | 修正为 `frameX == 90` 精确判定 | ✅ 已闭环 |
| P2 | 战利品扫描在视口裁剪前无条件执行导致主线程掉帧 | 新增 `IsPositionInViewport` 廉价几何预裁剪 | ✅ 已闭环 |
| P3 | `markLivingTree`/`markUnderworld` 死开关及 `markDungeon` 文案歧义 | 清理死开关，收敛文案为"地牢地表主入口" | ✅ 已闭环 |
| P4 | 后台线程直接读写游戏共享状态与非线程安全调用 `Main.NewText` | 引入 `ConcurrentQueue<Action>` 主线程队列（`UpdatePostfix` 消费）+ 标量状态快照入参 + `ScopedTimer` 耗时监测 | ✅ 已闭环 |
| P5 | 神庙大门 2 宽图格导致左右重复生成 2 个重叠 pin | 增加 `frameX % 36 == 0` 仅保留左列子格 | ✅ 已闭环 |
| P6 | 宝箱样式映射缺失及 13 处映射错误（如误用非宝箱物品 ID） | 建立 Containers 0~51 (52种) + Containers2 0~37 (38种) 完整结构表驱动映射，原版 API 动态兜底 | ✅ 已闭环 |

---

## 3. P4 & P6 具体改造实现

### 3.1 P4：后台线程与主线程安全调度
- **主线程任务队列**：`ConcurrentQueue<Action> _mainThreadActions` 在 `PatchMain.UpdatePostfix` 逐帧安全消费，避免跨线程调用 `Main.NewText` 导致崩溃；
- **状态快照参数隔离**：`TriggerRescan()` 在触发主线程同步捕获 `maxX`, `maxY`, `worldSurface`, `rockLayer`, `dungeonX`, `dungeonY`, `chestSnapshot` 标量/引用快照并传入 `ScanWorldStructures`；
- **耗时监控**：引入 `TPML.Core.Diagnostics.ScopedTimer`（绑定 `LogManager.GetLogger("OptimizeAndTool")`），自动输出扫描耗时日志；
- **健壮性**：`IsTileActiveAndType` 增加 `tile == null` 防撕裂保护。

### 3.2 P6：宝箱样式表补全与 13 处纠错
- **Containers (TileID.Containers=21, style 0~51)**：
  - 修正 Containers 44（黑曜石箱 `ItemID.ObsidianChest` 2618）、45（南瓜箱 2619）、46（阴森箱 2620）、47（玻璃箱 2748，原为音乐盒 3237）；
  - 补全 18~22（地牢未锁五大环境箱 1528~1532）、23~27（地牢锁住环境神器箱 1533~1537 钥匙图标）、32（发光蘑菇箱 2544）、35~40（红/绿/蓝地牢箱与钥匙锁箱）、41（骨头箱 2615）、48~51（火星/陨铁/花岗岩/大理石箱）。
- **Containers2 (TileID.Containers2=467, style 0~37)**：
  - 修正 Containers2 0（水晶箱 3884）、1（黄金箱 3885，原为红宝石块 4644）、3（病变箱 3965）、5（日耀箱 4153）、10（沙漠箱 4267）、11（竹宝箱 4574）、13（地牢沙漠钥匙锁箱 4714）、15（气球箱 5177）、16（灰烬木箱 5198）；
  - 补全 6~8（漩涡/星云/星尘四柱箱）、9（高尔夫箱 4265）、12（地牢沙漠箱 4712）、14（珊瑚箱 5156）、17~37（以太/陨星/仙灵木/神圣家具/哥特/魔金/猩红/雪原/松木/巨石等 1.4.4 全系列宝箱）。
- **陷阱识别**：
  - `TileID.FakeContainers` / `TileID.FakeContainers2` 与 Containers2 style 4（死人宝箱）统一标注 `isTrapped = true`，红框警示；
  - 排除普通宝箱的陷阱误判。

---

## 4. 验证结果

- **子工程构建**：
  `dotnet build tPlainModLoader/Mods/OptimizeAndTool/OptimizeAndTool/OptimizeAndTool/OptimizeAndTool.csproj -c Release`
  **0 警告 0 错误**，部署成功。
- **全量解决方案构建**：
  `dotnet build tPlainModLoader/tPlainModLoader/tPlainModLoader.sln -c Release -m /graph`
  **20 个工程全部 0 警告 0 错误**，全自动热部署成功。