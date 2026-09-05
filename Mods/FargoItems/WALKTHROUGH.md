# FargoItems 迁移与 TPML 核心弹幕生命周期修复总结

本文档记录了本阶段在 **TPML 统一核心库** 与 **FargoItems 原生模组矩阵** 中完成的分批迁移、底层 Bug 根因剖析与彻底修复成果。

---

## 1. 核心问题根因诊断与框架级修复 (TPML Core Fixes)

### 1.1 弹幕生命周期与死亡钩子丢失 (`Hook_Kill`)
- **根因**：原版泰拉瑞亚在 `Projectile.Update()` 中处理超时（`timeLeft <= 0`）时，在调用 `Kill()` 前会将 `self.active` 标记为 `false`。而此前 `ProjectileLoader.Hook_Kill` 含有 `if (self != null && self.active)` 判断，导致所有生命周期为 1 帧的构建/爆炸类弹幕在死亡时**被框架完全跳过 `modProj.OnKill`**。
- **修复**：移除 `self.active` 限制，改为基于实例的弱引用穿透派发，确保弹幕无论因碰撞、超时还是代码调用被销毁时，**100% 触发 `ModProjectile.OnKill`**。

### 1.2 原版 AI 篡改模组弹幕类型 (`Hook_AI`)
- **根因**：Fargo 建筑/炸弹类弹幕配置了 `aiStyle = 16`（原版炸药 AI）。在 `timeLeft <= 3` 时，原版 `AI_016` 会强制执行 `this.type = 29;`（篡改为原版炸药），导致后续被当作原版弹幕而漏掉 `OnKill`。
- **修复**：在 `Hook_AI` 中加入 **防篡改保护（Anti-Type-Tampering）**，原版 AI 执行后自动将 `self.type` 强制复原为正确的模组弹幕 ID。

### 1.3 弹幕对象池槽位复用污染 (`Hook_SetDefaults` / `GetModProjectile`)
- **根因**：原版泰拉瑞亚固定预分配了 1000 个弹幕对象（`Main.projectile[1000]`）。当模组弹幕死亡后，该槽位被原版流星/雨滴/粒子复用，但框架此前在原版 `SetDefaults` 时未清除旧字典映射，导致落星在全图任意位置死亡时误触发上一个模组弹幕的 `OnKill`。
- **修复**：
  1. 在 `Hook_SetDefaults` 中，若槽位被原版弹幕占用（`Type < ModProjectileOffset`），立即执行 `_modProjInstances.Remove(self)`；
  2. 在 `Hook_Kill` 结束后的 `finally` 块中立即解绑当前销毁弹幕的实例缓存；
  3. 在 `GetModProjectile` 中增加 `instance.Type == proj.type` 校验。

### 1.4 挥发性工具射击时序与物品自动消耗 (`ItemLoader` / `ContentHookDispatcher`)
- **根因**：原版 `Player.ItemCheck_Shoot` 在 `useAnimation == useTime` 时恒不触发射击；且缺乏框架级自动消耗堆叠支持。
- **修复**：
  1. 在 `Hook_ItemCheck_StartActualUse` 中，对所有带有 `Item.shoot > 0` 的模组物品主动分发 `ItemLoader.Shoot`，并在 `Hook_ItemCheck_Shoot` 中拦截原版避免重复；
  2. 在 `ItemLoader.UseItem` 与射击管线中接入框架级 `consumable` 自动扣料。

---

## 2. FargoItems 模块分批迁移清单

本次累计为 `FargoItems` 独立模组迁移了 **90+ 项** 高频纯物品与配套弹幕：

### 📦 Batch 1: 无限弹药 (Ammos) & 时装外观 (Vanity)
- **无限箭袋 (11款)**：木箭、烈焰、冰霜、恶魔、神圣、诅咒、灵液、叶绿、夜明、穿云、骨箭袋。
- **无尽子弹袋 (13款)**：火枪、银、钨、金、流星、派对、高速、晶体、纳米、爆破、诅咒、灵液、叶绿、夜明子弹袋。
- **钱币袋 (4款)** & **飞镖盒 (4款)** & **火箭箱 (8款)**。
- **凝胶包、坠星袋、脆骨**。
- **NPC 时装套 (15件)**：突变体套、憎恶套、毁灭者套、伐木工套、巨蟹眼镜。

### 🏗️ Batch 2: 建筑/清图工具 (Explosives) & 环境重塑 (Renewals)
- **铺桥与铁轨**：小型泥土桥 (`MiniDirtInstaBridge`)、小型木桥 (`MiniInstaBridge`)、全图木桥 (`InstaBridge`)、黑曜石桥 (`ObsidianInstaBridge`)、双层黑曜石桥 (`DoubleObsidianInstabridge`)、全图铁轨 (`InstaTrack`)、桥梁转换器 (`OmniBridgifier` / `SemiBridgifier`)。
- **一键建筑与水池**：快速房屋 (`AutoHouse` / `AutoHouseProj`)、一键水池 (`InstaPond` / `InstaPondProj`)。
- **清图炸弹**：城市毁灭炸弹 (`CityBuster`)、墓地清理炸弹 (`GraveBuster`)、祭坛粉碎者 (`AltarExterminator`)、神庙虚化炸弹 (`LihzahrdInstactuationBomb`)、爆破手里剑 (`BoomShuriken`)。
- **环境重塑球 (16款)**：腐化、猩红、神圣、蘑菇、纯净、冰雪、沙漠、泥土（普通与至尊）。

### ⚔️ Batch 3: 便携 Boss 召唤物 (Summons) & 实用小工具 (Misc)
- **原版 12 款便携召唤物**：克眼、世吞、克脑、蜂王、巨鹿、史莱姆王、双子魔眼、毁灭者、机械骷髅王、石巨人、猪鲨、月总召唤物。
- **实用杂项小工具**：全图探索器 (`MapViewer`)、旅途全解锁 (`InstantResearch`)、便携日晷 (`PortableSundial`)、晶塔净化 (`PylonCleaner`)、红水晶 (`KohaCrystal`)。

---

## 3. 验证与测试结果

| 验证项 | 验证方式 | 结果 | 证据状态 |
| :--- | :--- | :---: | :---: |
| **全量解决方案构建** | `dotnet build tPlainModLoader.sln -c Release -m /graph` | ✅ 0 错误 0 警告 | `[构建验证]` |
| **自动化逻辑单元测试** | `dotnet run --project Scripts/FargoItems.Tests.csproj` | ✅ 全部通过 | `[构建验证]` |
| **快速房屋实机生成** | 包含地狱/太空高度层优先级与 1 格像素对齐 | ✅ 通过 | `[日志证据]` |
| **小型泥土平台实机生成** | 解决空中成型与 150 格强制放置 | ✅ 通过 | `[日志证据]` |
| **对象池复用隔离** | 清理落星/粒子对槽位的脏数据复用 | ✅ 通过 | `[日志证据]` |

---

## 4. Git 提交记录

- **子模块 `tPlainModLoader`**：`feat(tPlainModLoader,FargoItems): 迁移 FargoItems 核心矩阵并修复 ProjectileLoader 弹幕生命周期与对象池复用`
- **主仓库 `Cirno9TerrariaMods`**：`feat(tPlainModLoader): 更新子模块，收敛 FargoItems 迁移与 TPML 弹幕生命周期修复`

---

## 5. FargoItems 便携 Boss 召唤物重构与环境维持修复

### 5.1 根因分析与问题清单
1. **浓缩松露虫精华 (`TruffleWorm2`)**：原版 `NPC.SpawnOnPlayer(plr, 370)` 强制要求玩家存在激活的钓鱼浮漂（bobber），无浮漂直接静默 return 导致手持使用完全无反应。
2. **蜥蜴电池包 (`LihzahrdPowerCell2`)**：原版 `NPC.SpawnOnPlayer(plr, 245)` 强制要求周围 20 格存在蜥蜴祭坛，在神庙外使用直接失效。
3. **机械魔眼 (`MechEye`)**：硬编码了 `int type = 0;`，且未同时生成激光眼与魔焰眼。
4. **全生物群落维持 (`WormyFood` / `GoreySpine` / `DeerThing2` / `Abeemination2`)**：原版 Boss AI 在脱离对应群落时会立即下潜逃跑或暴怒。

### 5.2 实施方案
1. **创建 `FargoSummonHelper` 统一调度**：
   - 定向生成：猪鲨在玩家左右侧高空直接 `NPC.NewNPC`；石巨人向上扫描安全空旷方块坐标；双子魔眼同时生成激光眼与魔焰眼；
   - 网络支持：多人模式下向服务端发送 `PacketId_SummonBoss` 数据包，由服务端统一安全生成并广播 `Announcement.HasAwoken` / `LegacyMisc.48`。
2. **创建 `FargoItemsBiomePlayer : ModPlayer`**：
   - 监听世吞、克脑、蜂王、巨鹿等 Boss 存活状态并在玩家附近维持 `ZoneCorrupt / ZoneCrimson / ZoneJungle / ZoneSnow`，彻底兑现“在任何生物群落中召唤”且不逃跑/不暴怒的设定。
3. **重构 12 款召唤物物品类**：统一接入 `FargoSummonHelper`，并在 `CanUseItem` 中严格校验 Boss 唯一性与时间条件。
