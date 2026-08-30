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

---

# QoL 细节补齐（对齐 ImproveGame v1.8.2）实施记录

> 方案来源：`IMPLEMENTATION_PLAN.md`；依据：本地反编译 `TModSource\ImproveGame\v1.8.2` 功能清单 + `GameSource\Terraria` 原版挂载点调研（3 个子代理并行）。

## 第一批：消耗 / 掉落 / 死亡类（12 项，已完成）

| # | 功能 | 实现文件 | 原版挂载点（依据 GameSource） |
|---|---|---|---|
| 1 | 召唤物不消耗 | `Content\QoL\NoConsumeItems.cs` | `Player.CanConsumeConsumableItem`（Player.cs:5488，原版消耗豁免钩子，tML ConsumeItem 等价点）；ID 表对齐 ImproveGame `Lookups.BossSummonItems/EventSummonItems` |
| 2 | 无限弹药（≥3996） | 同上 | `Player.PickAmmo`（Player.cs:54319 统一扣减），Prefix 快照 58 格堆叠 + Postfix 恢复 |
| 3 | 无限投掷物（≥3996） | 同上 | `Player.CanConsumeConsumableItem`（43610 通用消耗点） |
| 4 | 无限电线（≥3996） | 同上 | 单格 `Player.ItemCheck_UseWiringTools`（47184 直扣，快照恢复）+ 批量 `Player.ConsumeItem`（9355，Prefix 放行） |
| 5 | 旗帜杀敌数倍率 | `Content\QoL\BannerAndBestiary.cs` | `NPC.CountKillForBannersAndDropThem` → `BannerSystem.AddNPCKillBy`（BannerSystem.cs:243 读 `ItemID.Sets.KillsToBanner`），Prefix 临时改需求 + Postfix 恢复 |
| 6 | 图鉴一次击杀全解锁 | 同上 | `NPCKillsTracker.RegisterKill` Postfix → `SetKillCountDirectly(id, GetKillCountNeeded(id))`（CommonEnemyUICollectionInfoProvider:61 达满即全信息） |
| 7 | 史莱姆必定含物品 | `Content\QoL\SlimeAndLava.cs` | `NPC.AI_001_Slimes` Prefix（原版 60951 概率块前直接调用 `AI_001_Slimes_GenerateItemInsideBody` 写入 ai[1]） |
| 8 | 熔岩史莱姆不产熔岩 | 同上 | `NPC.HitEffect`：type 59 临时置 `Main.remixWorld=true`（86561 的 `!remixWorld` 判 false）；GFB 蝙蝠 60/151 临时置 `Main.getGoodWorld=false`（86602） |
| 9 | 禁止墓碑 | `Content\QoL\DeathAndDamage.cs` | `PatchPlayer.CanDropTombstone` → `Player.DropTombstone` Prefix（只影响墓碑，不影响掉钱） |
| 10 | 满血复活 | 同上 | `Player.Spawn` Prefix 置 `spawnMax=true`（37889 满血满蓝分支，原版为死代码；普通复活仅半血） |
| 11 | 禁用伤害波动 | 同上 | `Main.DamageVar` Prefix 固定返回原值（67145，全局伤害浮动唯一入口） |
| 12 | 加速提炼机（×10） | `Content\QoL\FasterExtractinator.cs` | `Player.PlaceThing_ItemInExtractinator` Prefix 临时缩小 `useTime/10`（ApplyItemTime 依 useTime 定间隔） |

**接线**：`SettingUI.cs`（Data/Load/GetSaveData/SetDefault/OnValUpdate 五段）、`Content\Function.cs`（GetCO/GetUI 新增分组"消耗、掉落与死亡规则"）。
**验证**：`dotnet build OptimizeAndTool.csproj -c Release` **0 警告 0 错误**，自动部署成功。
**设计决策**：新功能默认关闭（`GetSetReset(false,false)`），不改变原版行为，用户按需开启。
**风险说明**：全部为本地状态拦截（Prefix/Postfix 成对恢复），不涉及存档/世界数据写入；多人模式下消耗判定在客户端本地执行，服务器裁决逻辑未改动。

## 第二批：生态 / 经济 / 城镇类（8 项已完成，3 项暂缓）

| # | 功能 | 实现文件 | 原版挂载点 |
|---|---|---|---|
| 13 | 南瓜迅速生长 | `Content\QoL\EcoGrowth.cs` | `WorldGen.GrowPumpkin` Postfix 防递归连调 3 次（52958，一次触发长满 4 阶段） |
| 14 | 生命果迅速生长 | 同上 | `WorldGen.PlaceJunglePlant` Postfix（type==236 时 60% 概率附近补种；原版概率 1/40/1/30） |
| 15 | NPC 钱币掉落倍率 | `Content\QoL\Economy.cs` | `PatchNPC.SetDefaultsPostfix` 改写 `NPC.value`（NPCLoot_DropMoney 80438 按 value 计算） |
| 16 | 死亡保存增益 | `Content\QoL\KeepBuffsOnDeath.cs` | `Player.UpdateDead` Prefix 快照 buffType/buffTime + Postfix 写回（17109 清空非持久 buff） |
| 17 | 专家 Debuff 时长还原 | `Content\QoL\ExpertDebuffTime.cs` | `Player.AddBuff_DetermineBuffTimeToAdd` Prefix 返回原时长（5426，×2/×2.5 换算） |
| 19 | 城镇 NPC 刷新速度乘数 | `Content\QoL\TownNPCSpawnSpeed.cs` | `WorldGen.TrySpawningTownNPC` Prefix 本帧缩小 npcSpawnPeriod /倍率（75481） |
| 22 | 禁止邪恶蔓延（配置化） | `Content\QoL\NoBiomeSpread.cs` | `PatchWorldGen.CanConvert` 拦截 conversionType 1/2/4（腐化/神圣/猩红），保留 8/9/11 |
| 23 | 无条件队内传送 | `Content\QoL\NoConditionTeamTP.cs` | `Player.CanWormholeToSpectating` Prefix 恒真 + `TakeUnityPotion` Prefix 不消耗 |

**暂缓（复杂度高或需 UI 面板）**：旅行模式自动研究（需 CreativeUI/网络同步）、旅商商店刷新按钮（需 Patch NPC 对话 UI）、中键坐骑（需 UI 层中键捕捉）。

**接线**：`SettingUI.cs` 五段 + `Function.cs` 分组"生态、经济与传送规则"。
**验证**：子工程构建 **0 警告 0 错误**，自动部署成功。

## 第三批：床 / 晶塔 / 多人协作（8 项已完成）

| # | 功能 | 实现文件 | 原版挂载点 |
|---|---|---|---|
| 24 | 床任意位置设重生点 | `Content\QoL\BedRules.cs` | `Player.CheckSpawn` Prefix：是床即 true（55190，跳过 StartRoomCheck 房间判定） |
| 25 | 无视睡觉限制 | 同上 | `PlayerSleepingHelper.DoesPlayerHaveReasonToActUpInBed` Prefix 恒 false（45-64） |
| 26 | 睡觉时间速率 | 同上 | `Main.UpdateTimeRate` Postfix 覆写 dayRate（6377，全睡时 ×5 → 自定义倍率） |
| 27 | 一人睡觉即可加速 | 同上 | `Main.UpdateTimeRate` Prefix 伪造 SleepingPlayersCount（6387 全睡判定） |
| 28 | 晶塔传送无需 NPC | `Content\QoL\PylonRules.cs` | `TeleportPylonsSystem.DoesPositionHaveEnoughNPCs` Postfix 恒真（224） |
| 30 | 晶塔传送无视群落 | 同上 | `TeleportPylonsSystem.DoesPylonAcceptTeleportation` Prefix 恒真（254） |
| 36 | 失焦保持游戏运行 | `Content\QoL\KeepRunningWhenUnfocused.cs` | `FocusHelper.UpdateFocus` Prefix 强制 wantsToPause=false（133） |
| 37 | 队伍共享便携制作站 | `Content\QoL\TeamShare.cs` | `Player.AdjTiles` Postfix 合并同队玩家 adjTile（35940；原版无 team 维度支持） |

**剔除说明**：晶塔"无视危险"——调研确认原版晶塔传送本身无危险/Boss 检查，无需实现；"显示物品所属模组"——原生 TPML 环境物品均出自原版，无模组概念。

**接线**：`SettingUI.cs` 五段 + `Function.cs` 分组"床、晶塔与多人协作"。
**验证**：子工程构建 **0 警告 0 错误**，自动部署成功。

## 审查修复记录（自查 + 子代理复核前）

| # | 问题 | 修复 |
|---|---|---|
| 1 | `Patch_OnePlayerSleep` 与 `Patch_BedTimeRate` 各自独立挂 `Main.UpdateTimeRate`，两个 Postfix 的执行顺序由反射发现顺序决定；若恢复计数的 Postfix 先跑，倍率判定会读到已恢复值而失效 | 合并为单一 `Patch_SleepTimeRate`（Prefix 伪造全睡计数 → Postfix 先按当前计数应用倍率 → 再恢复计数），顺序自控 |
| 2 | `Patch_LavalessLavaSlime` 在 GFB 世界（`Main.remixWorld` 本为 true）杀熔岩史莱姆时，Postfix 会把 `remixWorld` 错误恢复成 false，破坏世界状态 | Prefix 增加 `!Main.remixWorld` 前置条件，仅在原本为 false 时改写并登记恢复（子代理复核通过） |
| 3 | `Patch_KeepRunningWhenUnfocused` 用 Prefix 跳过 `FocusHelper.UpdateFocus`，导致 `IsSelectedApplication`（星落/变色/任务栏闪烁等属性依赖）与鼠标可见性状态不再更新 | 改为 Postfix 覆写 `wantsToPause=false`，原方法完整执行仅改结果 |

## 独立审查结果（子代理完整复核）与追加修复

**审查结论**：28 项全部落地、五段接线完整、编译 0 警告 0 错误；挂载点签名与原版反编译源码逐一比对，无签名错配；WALKTHROUGH 行号引用全部准确。

**本次新增功能相关修复（审查后追加）**：

| # | 问题 | 修复 |
|---|---|---|
| 4 | Bug#8：静态快照在原方法抛异常时不恢复（Postfix 不随异常执行），KillsToBanner/useTime/npcSpawnPeriod/SleepingPlayersCount/snapshot/buffType 等全局或玩家状态可能残留 | 7 处关键恢复逻辑由 `[HarmonyPostfix]` 升级为 `[HarmonyFinalizer]`（无论正常/异常均执行恢复）：BannerAndBestiary、FasterExtractinator、TownNPCSpawnSpeed、BedRules、NoConsumeItems（弹药/电线各一）、KeepBuffsOnDeath |
| 5 | Bug#9：`TownNPCSpawnSpeed.Multiplier` 初始 2f/重置 1f 与 Data 默认 2f 不一致（全新=2 倍，恢复默认=1 倍）；`BedRules.BedTimeRate` 初始 10f/重置 5f 同理 | 统一为初始=重置=Data：`Multiplier (2f,2f)`、`BedTimeRate (10f,10f)` |
| 6 | Bug#18：无限电线同时放行批量布线消耗的作动器 849（Wiring.cs:447/451），UI 文案未提及 | 文案更新为"电线/作动器（批量布线）" |

**既有功能发现的 bug（未在本次范围，待用户决策）**：
- 🔴 无；🟠 Bug#2 钱币堆叠溢出 maxStack=100（PortableContainer，默认开启直接暴露）；Bug#3 自动重铸固定计费；Bug#4 VeinMining 无视稿力 + 多人网络洪泛；Bug#5 TownNPC EnableAutoHouse 默认开启全解锁；Bug#6 渔夫无冷却默认开启；Bug#7 钓鱼切物保护按 selectedItem 恢复可能写错槽
- 🟡 Bug#10-17（硬编码中文快乐报告、Pipette 空 catch、FishingInfoHUD 浮标选取、Buff tooltip 重绘漂移、房屋面板右键冲突、AdjTiles 每帧扫描、InfiniteBuffWindow 事件泄漏等）

## 既有功能 bug 修复记录（用户确认全量修复）

| # | 问题 | 修复 |
|---|---|---|
| Bug#2 | `PortableContainer` 自动存钱按 maxStack=9999 计算槽位/写入，钱币实际 maxStack=100，≥101 铂金写入非法堆叠（101/100） | `slotsNeeded` 与 `stackToPut` 改用 100（加注释说明原版 Item.cs case 71-74） |
| Bug#3 | `PerformAutoReforge` 循环前算一次固定费用，与原版"按当前物品价值逐次计费"不等价 | 计费移入循环内，每次 roll 前 `GetSingleReforgeCost` 重算 |
| Bug#4 | VeinMining 连锁挖掘不校验稿力（铜镐可挖叶绿/精金）；多人每块 SendData 网络洪泛 | 复用原版 `Player.PickTile_DetermineDamage`（damage≤0 跳过）；多人限单次连锁量 200 |
| Bug#5 | `EnableAutoHouse` 默认开启且无条件置位全部 saved* 解锁标记（跳过救援前置），另含重复代码 | 默认值降为 false；删除 savedBartender/savedStylist 重复行；无条件解锁段加注释说明 |
| Bug#6 | `EnableNoAnglerCooldown` 默认开启，配合任务鱼堆叠可无限交付刷奖励 | 默认值降为 false |
| Bug#7 | AutoFishing 切物保护 Prefix 篡改 held 字段、Postfix 按 `inventory[selectedItem]` 恢复，切换槽位会写错格 | `BobberHoldState` 增加 `FakedItem` 引用，Prefix 记录、Postfix 直接对引用恢复（浮标 AI 与 DrawProj 两处） |
| Bug#10 | 快乐报告硬编码中文，非中文环境显示中文 | 仅 `GameCulture.CultureName.Chinese` 活跃时覆写 |
| Bug#11 | Pipette `ReturnBigBagItemToStorage` 空 catch 吞异常 | 补 `TPML.Core.Logging` 错误日志 |
| Bug#12 | FishingInfoHUD 浮标选取逻辑 `p.wet` 判断失效（第一个活跃浮标恒被选中） | 两遍扫描：优先入水浮标，无则取第一个 |
| Bug#15 | PortableCraftingStation `AdjTilesPostfix` 节流导致制作列表闪烁 | 重构为"15 tick 周期性全量扫描更新缓存 + 逐帧常驻快速合并"机制，制作站状态每帧稳定生效，根除 Crafting UI 闪烁 |
| Bug#17 | InfiniteBuffWindow `OnOpen` 每次 `+= Rebuild` 重复订阅事件 | 先 `-=` 再 `+=` |
| Bug#13 | Buff tooltip 完全替换原版重绘（版本升级漂移风险） | **评估保留**：当前逐行比对一致、功能正常，重构为叠加渲染属高风险改动，收益低 |
| Bug#14 | 房屋面板右上角右键消费区域可能与列表交互冲突 | **评估保留**：该区域（EquipPage==1 + screenWidth-70 顶部）本就专属房屋管理按钮，原版列表条目不在此区域 |
| Bug#19 | 天顶世界/颠倒世界图格 null 引发 `Player.AdjTiles` 空指针异常并中断主更新循环（导致开背包无法移动、吸钱币停滞、开闭设置崩溃） | `Patch_Player.AdjTilesPrefix` 框架级全量安全接管与诊断探针，引入 `PlayerAdjTileExtensions` 安全扩展方法，彻底杜绝 NRE 崩溃 |

**验证**：子工程与全量 sln 构建均 **0 警告 0 错误**，自动热部署成功。实机验证制作界面稳定无频闪，天顶世界背包移动与图格扫描完全正常。

---

# Boss 宝藏袋与钓鱼宝匣开箱永久全量掉落改造记录

> **方案对齐**：需求说明与原版 1.4.4.9 `GameSource` 反编译源码对照。

## 1. 改造内容与架构设计

1. **原版 17 种 Boss 宝藏袋永久全量掉落**：
   - 拦截 `Player.OpenBossBag`（`Prefix`），玩家开启任意 Boss 宝藏袋时，无视历史获取状态，直接全量喷出该 Boss 掉落池中的所有武器、专家专属饰品、坐骑、宠物、工具、面具与材料；
   - 9 种困难模式（肉山后）Boss 宝藏袋（双子魔眼、毁灭者、机械骷髅王、世纪之花、石巨人、猪鲨公爵、光之女皇、史莱姆皇后、月球领主）开启时，每次必定额外掉落 1 套随机完整的开发者套装（包含衣服+裤子/裙子+头饰/面具+翅膀+特有饰品/染料，共 21 套完整开发者套装）；
2. **钓鱼宝匣与开箱容器同步永久全量掉落**：
   - 拦截 `Player.OpenFishingCrate`、`OpenCanofWorms`、`OpenOyster`、`OpenLockBox`、`OpenShadowLockbox`；
   - 12 种钓鱼宝匣（木匣、铁匣、金匣、地牢匣、天空匣、丛林匣、腐化匣、猩红匣、神圣匣、冰冻匣、绿洲匣、海洋匣及其肉后版本）、金锁盒、黑曜石锁盒、生蚝（白/黑/粉珍珠）、蠕虫罐头每次开启均掉落全部专属物品；
3. **小怪与 Boss 死亡本体掉落**：
   - 保持原有的 Anti-RNG 首见保底机制（`ItemDropResolver.ResolveRule` 拦截，首次击杀掉齐未拥有战利品，后续击杀恢复原版概率）；
4. **强类型与 ItemID 全量校对**：
   - 校对并替换所有不一致的物品 ID 常量，确保强类型与原版 1.4.4.9 完全对齐，杜绝编译错误与运行时异常；
5. **UI 文案更新**：
   - 更新 `GuaranteedDropSystem.cs` 的提示文案与设置标题，清晰传达掉落优化与开箱全量大爆机制。

## 2. 编译构建验证

- **子工程构建**：
  `dotnet build tPlainModLoader/Mods/OptimizeAndTool/OptimizeAndTool/OptimizeAndTool/OptimizeAndTool.csproj -c Release`
  - 结果：**0 警告 0 错误**，生成并自动热部署至游戏目录。
- **全量解决方案构建**：
  `dotnet build tPlainModLoader/tPlainModLoader/tPlainModLoader.sln -c Release -m /graph`
  - 结果：**20+ 个工程全部 0 警告 0 错误**，极速多核图构建完成，全部自动热部署成功。

---

# OptimizeAndTool 全量 Harmony 清零与 HookGen 强类型门控重构记录

> **日期**：2026-08-30  
> **作者**：SaintCirno9  
> **核心目标**：全面废弃 Harmony 补丁，全量迁移至 `TerrariaHooks` HookGen 强类型 `On_` / `IL_` 门面系统，并彻底移除 `HarmonyX` 依赖。

## 1. 核心变更总结
1. **彻底移除 HarmonyX**：从 `OptimizeAndTool.csproj` 移除 `<PackageReference Include="HarmonyX" />`，消除全部 `MethodInfo` / `AccessTools` / `HarmonyPatch` 反射和字符串挂钩；
2. **45 个历史文件命名标准化**：消除遗留 `Patch_` 前缀，标准化重命名为 `*Hooks.cs`，并提供显式对称的 `RegisterAll()` / `UnregisterAll()` 生命周期管理；
3. **全模块强类型门控接入**：
   - 渲染优化：`ReduceMouseLagHooks`、`GameViewMatrixZoomLimitHooks`、`SmartSelectRangeHooks`、`KeepRunningWhenUnfocused`、`PortableCraftingStation`；
   - 存储系统：`BigBagPickupHooks`、`BigBagShiftTransferHooks`、`HotbarScrollHooks`、`AccessoryBagInteractionHooks`、`ItemContainerInteractionHooks`、`PortableContainerHooks`；
   - 钓鱼系统：`AutoFishingSuppliesHooks`、`AutoFishingSystemHooks`、`FishingCatchProcessor`、`FishingCrateModifierHooks`、`FishingInfoHUDHooks`、`MultipleFishingLinesHooks`、`AnglerQuestOptimizationHooks`；
   - 物块与生态：`PlayerPickTileHooks`、`AntiGriefHooks`、`EcologyHooks`、`PylonHooks`、`PylonRuleHooks`、`EcoGrowthHooks`、`SlimeAndLavaHooks`、`BedRulesHooks`、`TownNPCOptimizationHooks`、`TownNPCSpawnSpeedHooks`、`FasterExtractinatorHooks`；
   - 玩家战斗与规则：`DeathAndDamageHooks`、`ExpertDebuffTimeHooks`、`KeepBuffsOnDeathHooks`、`NoConditionTeamTPHooks`、`NoConsumeItemHooks`、`TeamShareHooks`、`UncapMaxLifeHooks`、`ItemMaxStackHooks`、`BannerAndBestiaryHooks`；
   - 无限增益与掉落：`BuffInteractionHooks`、`InfinitePotionAndBuffHooks`、`GuaranteedDropHooks`、`ReforgeHooks`；
4. **统一生命周期收拢**：入口类标准化为 `OptimizeAndToolHookInit`，统一注册与注销所有 MonoMod 门控；
5. **日志清理**：移除了 `PortableCraftingStation.cs` 中的周期性激活统计日志。

## 2. 编译验证
- **全量解决方案构建**：`dotnet build tPlainModLoader/tPlainModLoader/tPlainModLoader.sln -c Release`
- **验证结果**：`[构建验证]` **0 警告、0 错误**，19 个工程全量构建完成并自动热部署至游戏目录。