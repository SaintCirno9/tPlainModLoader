# TPML tML Patch 扩展层：工作量评估与第一批落地计划

> 目标工程：`TPML.Content`  
> 作者：SaintCirno9  
> 配套架构：`docs/tPlainModLoader/TML_PATCH_EXTENSION_ARCHITECTURE.md` v1.1.0

---

## 1. 调研结论（已核实）

### 1.1 文档示例已过时，不能当待办清单

| 文档举例 | 现状 | 结论 |
| :--- | :--- | :--- |
| `Player.HasItem` / `CountItem` | 原版已有；TPML Fusion 已 `On_` 拦截 | **不要再写扩展** |
| `Player.GetModPlayer<T>` | `TPML.Content.ModPlayerExtensions` + `Terraria.ModLoader` 桥已有 | **不要再写扩展** |
| `Item.CountsAsClass` / `DamageType` | 依赖完整 `DamageClass` 体系（tML 独立子系统） | **本批不做**（单独大工程） |
| `Item.ModItem` 属性语法 | C# 无扩展属性；`ItemLoader.GetModItem` 已有静态查询 | 本批只补 **方法** `GetModItem()`，属性语法留给 Prepatcher |

### 1.2 547 个 patch 的真实结构

- glob 确认 `TModSource/tModLoader/patches/` 下确有 **547** 个 `.patch`。
- 其中相当一部分是 GoG / TerrariaNetCore / 平台 / csproj，**不是 API**。
- tML 真正对外的新增成员，主要在 `patches/tModLoader/Terraria/**/*.TML.cs`（约 90 个 partial 文件：`Player.TML.cs`、`Item.TML.cs`、`NPC.TML.cs`、`Utils.TML.cs`、`Recipe.TML.cs`…），而不是 547 个 hunk 里逐行 `+ public`。
- `Player.cs.patch` / `Item.cs.patch` 大量是：**改字段类型、改方法体、插入 Loader 调用**（类别 4），不能转成扩展方法。

### 1.3 工作量预估（人天，单人熟悉本仓库）

| 工作项 | 预估 | 说明 |
| :--- | :--- | :--- |
| 本批：不依赖 DamageClass 的高频扩展（HasBuff / CloneDefaults / CanAfford / NextBool 等） | **0.5 天** | 本次落地 |
| Manifest 脚本：扫描 `.TML.cs` + `.patch`，产出「新增方法 / 属性 / 改体」清单 | **0.5–1 天** | 自动发现，不自动实现 |
| 属性/字段语法（`item.ModItem`、`player.TalkNPC`） | **1–2 天** | 必须 Prepatcher 注入，扩展方法做不到 |
| `DamageClass` + `CountsAsClass` + `GetDamage` 全家桶 | **2–4 周** | 独立子系统，禁止塞进扩展层 |
| 按模组 triage 方法体改动（KillTile / NPCLoot 等） | **每模组 0.5–2 天** | 见架构文档 §5，不照单全收 547 |
| 「547 patch 全自动转译、一字不改」 | **不现实** | 方法体引用 tML 内部 API，只能脚手架 |

---

## 2. 本批范围（Phase A，已批准执行）

只做：**纯便捷方法、只读/浅写原版状态、零 DamageClass、零新框架类型**。

| API | 承载 | 语义来源 |
| :--- | :--- | :--- |
| `Player.HasBuff(int)` | 扩展方法 | `FindBuffIndex(type) != -1` |
| `Player.TryGetModPlayer<T>` | 扩展方法 | 只查已绑定实例，**不**走 `GetModPlayer` 的兜底实例化 |
| `Player.CanAfford(long, int)` | 扩展方法 | 对齐 tML：统计背包+四银行金币；自定义货币走 `CustomCurrencyManager` 计数但不扣款 |
| `Item.CloneDefaults(int)` | 扩展方法 | `SetDefaults` 后还原 `type`/`material` |
| `Item.GetModItem()` / `GetModItem<T>()` | 扩展方法 | 转发 `ItemLoader.GetModItem`（属性语法不做） |
| `Item.IsNotSameTypePrefixAndStack` | 扩展方法 | 转发原版 `IsNotTheSameAs` |
| `NPC.HasBuff(int)` | 扩展方法 | `FindBuffIndex(type) != -1` |
| `UnifiedRandom.NextBool` / `NextFloat(min,max)` / `Next<T>(list)` | `namespace Terraria` 扩展 | 对齐 `Utils.TML.cs`；`using Terraria` 即可 |
| `Vector2.ToWorldCoordinates` / `ToPoint16` / `Point.Deconstruct` / `Point16.ToPoint` | 同上 | 对齐 `Utils.TML.cs` 坐标转换 |

**明确不做（本批）：**

- `CountsAsClass` / `GetDamage` / `StatModifier`（DamageClass）
- `NPC.NewNPCDirect`（tML 是 `NPC` 上的静态方法，扩展无法写成 `NPC.NewNPCDirect`）
- `Item.NewItem(Rectangle)` 重载（与原版 `NewItem` 签名族冲突风险）
- `TalkNPC` / `ItemAnimationActive` 等**属性**（扩展方法无法提供无括号语法）
- 方法体改写类 patch（走架构文档 §5 triage，不在扩展层）

---

## 3. 命名空间策略

- 实体扩展（Player/Item/NPC）：`namespace TPML.Content`，与现有 `PlayerAdjTileExtensions` / `TileObjectExt` 一致；`Terraria.ModLoader` 经 `Compatibility.cs` 转发，供 `using Terraria.ModLoader` 的移植代码解析。
- 随机数/坐标：`namespace Terraria`（与 tML `partial Utils` 一致）。`Main.rand.NextBool()` 在仅 `using Terraria` 时即可编译。
- 不把同一签名同时放进 `TPML.Content` 与 `Terraria`，避免双 `using` 时 CS0121。

---

## 4. 验证

- `dotnet build tPlainModLoader/tPlainModLoader/TPML.Content/TPML.Content.csproj -c Release`
- 本批为编译期 API 补齐，无运行时行为变更；**不启动 GABS**。

---

## 5. 后续

1. **Manifest 扫描器（已落地）**：`Scripts/inventory_tml_patches.py`  
   产出 `docs/tPlainModLoader/TML_API_MANIFEST.md` + `.csv`。  
   用法：`uv run python Scripts/inventory_tml_patches.py`  
   数字是上界（字段/改可见性仍有噪声）；**真正可执行的是「高频类型 + extension」表**，不要按「仍缺 1914」开工。
2. 下一优先：只从 Manifest「扩展方法候选」里按正在移植的模组挑，不要再猜。
3. Prepatcher 注入高频属性（`Item.ModItem`）——扩展方法做不到。
4. `DamageClass` 仍为独立子系统，禁止塞进扩展层。
