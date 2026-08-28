using CommandHelp;
using HarmonyLib;
using OptimizeAndTool.Utils;
using OptimizeAndTool.Utils.quickBuild;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.UI;
using TPML.Content;
using TPML.Content.Fusion;

namespace OptimizeAndTool.Content.QoL
{
    /// <summary>
    /// 随身/便携制作站与水源补丁
    /// 遍历背包、便携收纳与所有框架级外部融合容器（如大背包），常驻激活其中的制作站与水源/熔岩/蜂蜜环境
    /// 作者: SaintCirno9
    /// </summary>
    [HarmonyPatch(typeof(Player))]
    internal class PortableCraftingStation
    {
        public static GetSetReset<bool> Enable = new GetSetReset<bool>(true, true);

        public static List<CommandObject> GetCO()
        {
            return new List<CommandObject>
            {
                CommandBuild.get2("portableCraftingStation", Enable)
            };
        }

        public static List<UIElement> GetUI()
        {
            return new List<UIElement>
            {
                UIBuild.get2(Enable, "随身携带（含大背包及外部融合容器）的制作站家具与水/岩浆/蜂蜜桶无需放置即可直接生效", "Images/Item_361", "便携制作站与水源")
            };
        }

        private static int scanTimer = 15;
        private static bool[] cachedAdjTile = new bool[TileID.Count];
        private static bool cachedAdjWaterSource = false;
        private static bool cachedAdjLava = false;
        private static bool cachedAdjHoney = false;
        private static bool cachedAlchemyTable = false;

        [HarmonyPatch(nameof(Player.AdjTiles))]
        [HarmonyPostfix]
        public static void AdjTilesPostfix(Player __instance)
        {
            if (__instance == null || !Enable.val || __instance.adjTile == null) return;

            try
            {
                // 1. 周期性扫描（每 15 tick 扫描一次背包与外部容器，更新缓存）
                if (++scanTimer >= 15)
                {
                    scanTimer = 0;
                    UpdateCachedAdjTiles(__instance);
                }

                // 2. 每一帧原版清空后，都把缓存的随身制作站状态快速合并给玩家（确保每帧稳定生效，彻底消除合成列表闪烁）
                ApplyCachedAdjTiles(__instance);
            }
            catch
            {
                // 防御性保护：避免制作站扫描异常中断游戏更新循环
            }
        }

        private static void UpdateCachedAdjTiles(Player player)
        {
            if (player == null) return;

            if (cachedAdjTile == null || cachedAdjTile.Length != TileID.Count)
            {
                cachedAdjTile = new bool[TileID.Count];
            }
            Array.Clear(cachedAdjTile, 0, cachedAdjTile.Length);
            cachedAdjWaterSource = false;
            cachedAdjLava = false;
            cachedAdjHoney = false;
            cachedAlchemyTable = false;

            // 1. 扫描原版玩家主背包与四箱便携收纳（猪猪/保险箱/护卫熔炉/虚空袋）
            ScanContainer(player.inventory);
            if (player.bank?.item != null) ScanContainer(player.bank.item);
            if (player.bank2?.item != null) ScanContainer(player.bank2.item);
            if (player.bank3?.item != null) ScanContainer(player.bank3.item);
            if (player.bank4?.item != null) ScanContainer(player.bank4.item);

            // 2. 扫描框架级所有已激活外部融合源（如大背包等）中的制作站与水源环境
            var sources = InventoryFusionManager.GetActiveSources(player);
            if (sources != null)
            {
                for (int s = 0; s < sources.Count; s++)
                {
                    var src = sources[s];
                    if (src == null) continue;
                    Item[] slots = src.GetSlots(player);
                    if (slots != null && slots.Length > 0)
                    {
                        ScanContainer(slots);
                    }
                }
            }
        }

        private static void ApplyCachedAdjTiles(Player player)
        {
            if (player?.adjTile == null || cachedAdjTile == null) return;

            int len = Math.Min(player.adjTile.Length, cachedAdjTile.Length);
            for (int i = 0; i < len; i++)
            {
                if (cachedAdjTile[i])
                {
                    player.adjTile[i] = true;
                }
            }
            if (cachedAdjWaterSource) player.adjWaterSource = true;
            if (cachedAdjLava) player.adjLava = true;
            if (cachedAdjHoney) player.adjHoney = true;
            if (cachedAlchemyTable) player.alchemyTable = true;
        }

        private static void SetCachedTile(int tile)
        {
            if (cachedAdjTile == null || tile < 0 || tile >= cachedAdjTile.Length) return;
            cachedAdjTile[tile] = true;

            if (tile == 355 || tile == 699)
            {
                cachedAlchemyTable = true;
            }

            if (Recipe.TileCountsAs != null && tile < Recipe.TileCountsAs.Length)
            {
                var list = Recipe.TileCountsAs[tile];
                if (list != null)
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        SetCachedTile(list[i]);
                    }
                }
            }
        }

        private static void ScanContainer(Item[] items)
        {
            if (items == null) return;

            for (int i = 0; i < items.Length; i++)
            {
                Item item = items[i];
                if (item == null || item.IsAir || item.stack <= 0) continue;

                // 制作站家具对应图格处理
                if (item.createTile >= 0)
                {
                    int tile = item.createTile;
                    SetCachedTile(tile);

                    // 原版水源环境家具识别 (如水槽/水泉等)
                    if (TileID.Sets.CountsAsWaterForCrafting != null &&
                        tile < TileID.Sets.CountsAsWaterForCrafting.Length &&
                        TileID.Sets.CountsAsWaterForCrafting[tile])
                    {
                        cachedAdjWaterSource = true;
                    }

                    // 高阶制作站向下兼任与特殊制作环境拓展
                    switch (tile)
                    {
                        case TileID.LivingLoom: // 生命木织机 (304) -> 兼任普通织布机 (86 Loom) 与基础工作台 (18 WorkBenches)
                            SetCachedTile(TileID.Loom);
                            SetCachedTile(TileID.WorkBenches);
                            break;

                        case TileID.AlchemyTable: // 炼金桌 -> 瓶子 + 炼金减免
                            if (TileID.Bottles < cachedAdjTile.Length) cachedAdjTile[TileID.Bottles] = true;
                            cachedAlchemyTable = true;
                            break;

                        case TileID.Hellforge: // 地狱熔炉 -> 普通熔炉
                            SetCachedTile(TileID.Furnaces);
                            break;

                        case TileID.AdamantiteForge: // 精金/钛金熔炉 -> 地狱熔炉 + 普通熔炉
                            SetCachedTile(TileID.Hellforge);
                            SetCachedTile(TileID.Furnaces);
                            break;

                        case TileID.MythrilAnvil: // 秘银/山铜砧 -> 铁砧
                            SetCachedTile(TileID.Anvils);
                            break;

                        case TileID.HeavyWorkBench: // 重型工作台 -> 基础工作台
                            SetCachedTile(TileID.WorkBenches);
                            break;

                        case TileID.Sinks: // 水槽 -> 水源环境
                            cachedAdjWaterSource = true;
                            break;

                        case TileID.Bottles: // 瓶子/玻璃杯
                        case TileID.TeaKettle: // 茶壶
                            if (TileID.Bottles < cachedAdjTile.Length) cachedAdjTile[TileID.Bottles] = true;
                            break;
                    }
                }

                // 液体环境识别 (水桶、熔岩桶、蜂蜜桶、无底桶、水瓶等)
                switch (item.type)
                {
                    case ItemID.WaterBucket:
                    case ItemID.BottomlessBucket:
                        cachedAdjWaterSource = true;
                        break;

                    case ItemID.LavaBucket:
                    case ItemID.BottomlessLavaBucket:
                        cachedAdjLava = true;
                        break;

                    case ItemID.HoneyBucket:
                    case ItemID.BottomlessHoneyBucket:
                        cachedAdjHoney = true;
                        break;

                    case ItemID.BottledWater:
                        cachedAdjWaterSource = true;
                        if (TileID.Bottles < cachedAdjTile.Length) cachedAdjTile[TileID.Bottles] = true;
                        break;

                    case ItemID.BottledHoney:
                        cachedAdjHoney = true;
                        if (TileID.Bottles < cachedAdjTile.Length) cachedAdjTile[TileID.Bottles] = true;
                        break;

                    case ItemID.Bottle:
                        if (TileID.Bottles < cachedAdjTile.Length) cachedAdjTile[TileID.Bottles] = true;
                        break;
                }
            }
        }
    }
}
