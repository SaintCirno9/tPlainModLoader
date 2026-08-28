# OptimizeAndTool 规则类 QoL 细节补齐计划（对齐 ImproveGame v1.8.2）

> **模组名称**：OptimizeAndTool（优化和工具）
> **适用版本**：Terraria 1.4.4+ / tPlainModLoader (`net472`)
> **作者**：SaintCirno9
> **依据**：本地反编译 `TModSource\ImproveGame\v1.8.2` 配置/物品本地化 + 源码目录；与现有 `OptimizeAndTool` 功能清单逐项对照
> **状态**：已确认范围（全量规则类，分多轮）

---

## 0. 范围说明与剔除项

- **已剔除**：重生加速（非Boss/Boss 百分比）——原版 1.4.4+ 已自带快速复活，无需实现。
- **待查证**：中键打开便携收纳 / 中键使用坐骑——若 1.4.4 原版已原生支持则不实现。
- **魔杖/建造工具系**：已有独立模组 WandsTool（泰之杖），不在本计划范围。
- **大功能物品**（自动钓鱼机/储存管理器/定位球/无人机/弹药链等）：超出"规则类 QoL 细节"范畴，不在本计划范围。

## 1. 实现模式（统一约定）

每项 = 一个新静态类（`[HarmonyPatch]` 特性类或 `PatchPlayer/PatchWorldGen/PatchMain/PatchNPC` 子类）+ 若干 `GetSetReset<T>` 配置 + `GetCO()` / `GetUI()` 暴露 + `SettingUI_player` 的 `Data`/`Load`/`GetSaveData`/`SetDefault` 四段接线 + `OnValUpdate` 自动保存监听。

- 新文件统一放 `Content\QoL\`（规则类）或 `Content\Cheat\QoL\`（原作弊区迁移配置化），每项一个文件。
- 若多个小项属于同一主题（如床系列、晶塔细分、无限消耗系），合并为一个文件 + 一个 `GetCO/GetUI`。
- UI 文案用 `UIBuild.get2(bool, 说明, 图标, 标题)` / `get1(bool, int, parse, 说明, 图标, 标题)`，图标尽量沿用 ImproveGame 同款物品图标（`Images/Item_N`）。
- 作者注释：`作者: SaintCirno9`。

## 2. 分轮计划（每轮结束 dotnet build 子工程 0 警告 0 错误）

### 第一批：消耗 / 掉落 / 死亡类（~12 项）
| # | 功能 | 实现要点 | 预计挂载 |
|---|---|---|---|
| 1 | 召唤物不消耗 | `Item.consumable` 召唤物（BOSS/事件召唤物）使用不扣堆叠 | Player.ItemCheck 相关 Prefix |
| 2 | 无限弹药 | 堆叠 ≥ 3996 的弹药不消耗 | Player.ConsumeAmmo / ammo 分支 |
| 3 | 无限投掷物 | 堆叠 ≥ 3996 的投掷物不消耗 | ItemCheck 投掷分支 |
| 4 | 无限电线 | 堆叠 ≥ 3996 的电线放置不消耗 | 电线放置扣减点 |
| 5 | 旗帜杀敌数倍率 | 击杀需求 = 原版需求 × 倍率（0.1 即 10%） | NPCLoot 旗帜判断 |
| 6 | 图鉴一次击杀全解锁 | 击杀一次即将该条目击杀数拉满 | BestiaryTracker.KillsTracker |
| 7 | 史莱姆必定含物品 | 史莱姆死亡必定掉落内含随机物品 | NPCLoot 史莱姆分支 |
| 8 | 熔岩史莱姆不产熔岩 | 专家/大师熔岩史莱姆、GFB 地狱蝙蝠死亡不喷熔岩 | 熔岩生成点 |
| 9 | 禁止墓碑 | 玩家死亡不掉落墓碑 | PatchPlayer.CanDropTombstone |
| 10 | 满血复活 | 复活时 HP = 生命上限（含 500+ 上限） | Player 复活点 |
| 11 | 禁用伤害波动 | 消除伤害 ±15% 随机浮动 | 伤害计算点 |
| 12 | 加速提炼机 | 提炼机使用/动画提速 | 提炼机入口 |

### 第二批：生态 / 经济 / 城镇类（~10 项）
| # | 功能 | 实现要点 |
|---|---|---|
| 13 | 南瓜迅速生长 | 南瓜生长加速 |
| 14 | 生命果迅速生长 | 生命果生长加速（可并入 13） |
| 15 | NPC 钱币掉落倍率 | NPCLoot 钱币 × 倍率 |
| 16 | 死亡保存增益 | 死亡不清除增益（buff 保留） |
| 17 | 专家 Debuff 时长还原经典 | 专家/大师 debuff 时长与经典一致 |
| 18 | 旅行模式自动研究 | 收藏物品达数量自动研究（不消耗） |
| 19 | 城镇 NPC 刷新速度乘数 | 入住/刷新速度 × 倍率 |
| 20 | 旅商商店可刷新 | 旅商对话面板加刷新按钮 |
| 21 | 中键坐骑（若原版无） | 中键直接使用坐骑 |
| 22 | 禁止邪恶蔓延（配置化） | 现有 stopTileConvert 作弊改为可配置项（PatchWorldGen.CanConvert） |
| 23 | 无条件队内传送 | 无需虫洞药水可队内传送 |

### 第三批：床 / 晶塔 / 显示 / 多人类（~12 项）
| # | 功能 | 实现要点 |
|---|---|---|
| 24 | 床：任意位置设置重生点 | 床不需要房间 |
| 25 | 床：无视睡觉限制 | 睡觉加速不被阻断 |
| 26 | 床：睡觉时间速率 | 时间流速乘数可调 |
| 27 | 床：一人睡觉即可加速 | 多人只需一人睡觉 |
| 28 | 晶塔：无需 NPC | 晶塔传送免 NPC 检查 |
| 29 | 晶塔：无视危险 | 晶塔传送免危险检查 |
| 30 | 晶塔：无视群落 | 晶塔传送免群落检查 |
| 31 | 墓地特效细分 | 迷雾 / 音乐独立开关（现有整体移除拆分） |
| 32 | 显示物品所属模组 | Tooltip 附加所属模组名 |
| 33 | 狱火圈 / 隐身不透明度 | 渲染透明度调节 |
| 34 | 洞穴探险药水高亮色 | 矿物高亮颜色可调 |
| 35 | 失焦保持游戏运行 | 单人失焦不暂停 |
| 36 | 队伍共享：制作站/增益/范围/自动红队 | 同队共享（多人，复杂，放最后） |

## 3. 文件变更清单

- 新增：`Content\QoL\NoConsumeItems.cs`（1~4）、`Content\QoL\BannerAndBestiary.cs`（5~6）、`Content\QoL\SlimeAndLava.cs`（7~8）、`Content\QoL\DeathAndDamage.cs`（9~11）、`Content\QoL\FasterExtractinator.cs`（12）……按批次逐步新增。
- 修改：`Content\Function.cs`（GetCO/GetUI 聚合）、`SettingUI.cs`（Data/Load/GetSaveData/SetDefault/自动保存）、（如涉及）`Command.cs`。
- 更新：`WALKTHROUGH.md`（每批完成后追加记录）。

## 4. 验证方式

- 每批：`dotnet build tPlainModLoader/Mods/OptimizeAndTool/OptimizeAndTool/OptimizeAndTool/OptimizeAndTool.csproj -c Release` 0 警告 0 错误；
- 全部完成后：`dotnet build tPlainModLoader/tPlainModLoader/tPlainModLoader.sln -c Release -m /graph` 全量验证；
- 逻辑核对以 GameSource 反编译源码为准（子代理调研结果落地到各实现文件注释）。

## 5. 风险与注意

- 消耗类 Patch 需谨慎处理 `netMode`（多人由服务器裁决），客户端模式只影响本地显示判断；
- 伤害波动 / 钱币倍率属数值类，改动面小，但需与原版公式对齐；
- 床/晶塔类已有部分实现（`pylonUnlimitedPlacement` / `pylonFreeTeleport`），细分项在其上扩展，不改动现有默认值语义；
- 队伍共享涉及多人同步，最后单独一轮实施。
