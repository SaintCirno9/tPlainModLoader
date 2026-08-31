# TPML.Content 扩展层 Phase A 走查

日期：本轮落地  
作者：SaintCirno9

## 做了什么

- 调研 547 个 tML patch 与现有 TPML API，确认文档举例 `HasItem`/`GetModPlayer` 已存在，不能当待办。
- 计划：`TPML.Content/IMPLEMENTATION_PLAN.md`。
- 实现不依赖 DamageClass 的高频扩展：
  - `Extensions/PlayerExtensions.cs`：`HasBuff`、`CanAfford`
  - `Extensions/ItemExtensions.cs`：`CloneDefaults`、`GetModItem`/`GetModItem<T>`、`IsNotSameTypePrefixAndStack`
  - `Extensions/NPCExtensions.cs`：`HasBuff`
  - `Extensions/UtilsExtensions.cs`（`namespace Terraria`）：`NextBool`/`NextFloat`/`Next<T>`、坐标转换
  - `ModPlayerExtensions.TryGetModPlayer<T>`（只查已绑定，不兜底实例化）
  - `Compatibility.cs` 转发到 `Terraria.ModLoader`

## 验证

```
dotnet build tPlainModLoader/tPlainModLoader/TPML.Content/TPML.Content.csproj -c Release
→ 成功生成，0 警告 0 错误
```

本批为编译期 API，未启动 GABS。

## 偏差

- 未实现文档早期举例的 `HasItem`/`GetModPlayer` 扩展（已有，重复会冲突）。
- 未实现 `CountsAsClass`（DamageClass 全家桶，单独评估）。
- 未实现属性语法 `item.ModItem`（扩展方法做不到）。

## Manifest 扫描器（本轮）

- 脚本：`Scripts/inventory_tml_patches.py`
- 产物：`docs/tPlainModLoader/TML_API_MANIFEST.md` / `.csv`
- 命令：`uv run python Scripts/inventory_tml_patches.py`
- 去重成员 2089；原版已有 161；TPML 已有 14（Phase A）；仍缺上界 1914；改体 hunk 884。
- 仍缺建议：prepatcher 1451（含字段噪声）/ extension 260 / facade 138 / framework 65。
