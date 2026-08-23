using CommandHelp;
using HarmonyLib;
using BigBagMod = OptimizeAndTool.Content.BigBag.BigBag;
using OptimizeAndTool.Utils;
using OptimizeAndTool.Utils.quickBuild;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.UI;

namespace OptimizeAndTool.Content.QoL
{
    /// <summary>
    /// 随身/便携制作站与水源补丁
    /// 遍历背包、便携收纳与巨大背包，常驻激活其中的制作站与水源/熔岩/蜂蜜环境
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
                UIBuild.get2(Enable, "随身携带（含巨大背包）的制作站家具与水/岩浆/蜂蜜桶无需放置即可直接生效", "Images/Item_361", "便携制作站与水源")
            };
        }

        [HarmonyPatch(nameof(Player.AdjTiles))]
        [HarmonyPostfix]
        public static void AdjTilesPostfix(Player __instance)
        {
            if (__instance == null || !Enable.val) return;

            // 扫描背包与便携收纳
            ScanContainer(__instance, __instance.inventory);
            if (__instance.bank?.item != null) ScanContainer(__instance, __instance.bank.item);
            if (__instance.bank2?.item != null) ScanContainer(__instance, __instance.bank2.item);
            if (__instance.bank3?.item != null) ScanContainer(__instance, __instance.bank3.item);
            if (__instance.bank4?.item != null) ScanContainer(__instance, __instance.bank4.item);

            // 扫描巨大背包中的制作站与水源环境
            if (BigBagMod.EnableBigBag.val && BigBagMod.EnableBigBagCraft.val && BigBagMod.Slots != null)
            {
                ScanContainer(__instance, BigBagMod.Slots);
            }
        }

        private static void ScanContainer(Player player, Item[] items)
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
                    if (tile < player.adjTile.Length)
                    {
                        player.adjTile[tile] = true;
                    }

                    // 高阶制作站向下兼容与关联制作环境处理
                    switch (tile)
                    {
                        case TileID.AlchemyTable: // 炼金桌 -> 瓶子 + 炼金减免
                            player.adjTile[TileID.Bottles] = true;
                            player.alchemyTable = true;
                            break;

                        case TileID.Hellforge: // 地狱熔炉 -> 普通熔炉
                            player.adjTile[TileID.Furnaces] = true;
                            break;

                        case TileID.AdamantiteForge: // 精金/钛金熔炉 -> 地狱熔炉 + 普通熔炉
                            player.adjTile[TileID.Hellforge] = true;
                            player.adjTile[TileID.Furnaces] = true;
                            break;

                        case TileID.MythrilAnvil: // 秘银/山铜砧 -> 铁砧
                            player.adjTile[TileID.Anvils] = true;
                            break;

                        case TileID.HeavyWorkBench: // 重型工作台 -> 基础工作台
                            player.adjTile[TileID.WorkBenches] = true;
                            break;

                        case TileID.Sinks: // 水槽 -> 水源环境
                            player.adjWaterSource = true;
                            break;

                        case TileID.Bottles: // 瓶子/玻璃杯
                            player.adjTile[TileID.Bottles] = true;
                            break;

                        case TileID.TeaKettle: // 茶壶
                            player.adjTile[TileID.Bottles] = true;
                            break;
                    }
                }

                // 液体环境识别 (水桶、熔岩桶、蜂蜜桶、无底桶、水瓶等)
                switch (item.type)
                {
                    case ItemID.WaterBucket:
                    case ItemID.BottomlessBucket:
                        player.adjWaterSource = true;
                        break;

                    case ItemID.LavaBucket:
                    case ItemID.BottomlessLavaBucket:
                        player.adjLava = true;
                        break;

                    case ItemID.HoneyBucket:
                    case ItemID.BottomlessHoneyBucket:
                        player.adjHoney = true;
                        break;

                    case ItemID.BottledWater:
                        player.adjWaterSource = true;
                        player.adjTile[TileID.Bottles] = true;
                        break;

                    case ItemID.BottledHoney:
                        player.adjHoney = true;
                        player.adjTile[TileID.Bottles] = true;
                        break;

                    case ItemID.Bottle:
                        player.adjTile[TileID.Bottles] = true;
                        break;
                }
            }
        }
    }
}
