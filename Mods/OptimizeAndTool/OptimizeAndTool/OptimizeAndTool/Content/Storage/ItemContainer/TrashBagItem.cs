using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.Creative;
using Terraria.ID;
using TPML.Content;
using TPML.Content.UI;

namespace OptimizeAndTool.Content.Storage.ItemContainer
{
    /// <summary>
    /// 随身垃圾桶 (TrashBag)
    /// 实体级独立收纳过滤容器，可容纳多达 200 种物品作为过滤样本；
    /// 存入其中的物品作为样本保存，随身携带（主背包或便携储物）时拾取同类物品将自动按商店标准价格（原价 20%）售卖为金币，
    /// 无售卖价值的物品就地销毁吞噬；数据通过 TPML Sidecar 伴随存档无损保存。
    /// 作者: SaintCirno9
    /// </summary>
    public class TrashBagItem : ItemContainerItem
    {
        public override string Name => "TrashBag";
        public override string Texture => "TrashBag";
        public override int Capacity => 200;
        public override string ContainerTitle => "随身垃圾桶";

        public override void SetStaticDefaults()
        {
            ItemLoader.SetDisplayName(Type, "随身垃圾桶");
            ItemLoader.SetTooltip(Type, "便携式过滤收纳垃圾桶，可存储多达 200 格物品作为过滤样本\n[c/88ff88:【操作提示】] 物品栏右键或悬停中键打开垃圾桶面板\n可将任意物品放入其中作为黑名单过滤样本\n随身携带且开启自动收纳时，拾取同类物品将[c/ffdd66:自动按商店卖价折算金币]\n拾取无价值垃圾物品（售价为0）时将[c/aaaaaa:直接就地销毁吞噬]\n放入垃圾桶中的样本不会被消耗，随时可以取出");
            CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
        }

        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.width = 48;
            Item.height = 42;
            Item.maxStack = 1;
            Item.rare = 10;
            Item.value = Item.sellPrice(0, 0, 50, 0);
            Item.consumable = false;
            Item.useStyle = ItemUseStyleID.None;
        }

        public override bool MeetEntryCriteria(Item item)
        {
            if (item == null || item.IsAir || item.type <= 0) return false;
            if (item.type == Type) return false; // 垃圾桶不能套娃放入自己
            return true;
        }

        public override bool OnPickupIntercept(Player player, Item item)
        {
            if (!AutoStorage || item == null || item.IsAir || item.type <= 0) return false;

            // 检查垃圾桶内是否有匹配该物品的过滤样本
            bool hasSample = false;
            if (Slots != null)
            {
                for (int i = 0; i < Slots.Length; i++)
                {
                    if (Slots[i] != null && !Slots[i].IsAir && Slots[i].type == item.type)
                    {
                        hasSample = true;
                        break;
                    }
                }
            }

            if (!hasSample) return false;

            int singleSell = item.value / 5;
            long totalSellPrice = (long)singleSell * item.stack;
            int stackCount = item.stack;
            int itemType = item.type;

            if (totalSellPrice > 0)
            {
                long remaining = totalSellPrice;
                int plat = (int)(remaining / 1000000);
                remaining %= 1000000;
                int gold = (int)(remaining / 10000);
                remaining %= 10000;
                int silver = (int)(remaining / 100);
                int copper = (int)(remaining % 100);

                void GiveCoins(int coinType, int count)
                {
                    if (count <= 0) return;
                    Item coinItem = new Item();
                    coinItem.SetDefaults(coinType);
                    coinItem.stack = count;
                    Item leftover = player.GetItem(coinItem, GetItemSettings.PickupItemFromWorld);
                    if (leftover != null && !leftover.IsAir && leftover.stack > 0)
                    {
                        player.QuickSpawnItem(player.GetItemSource_Misc(0), leftover.type, leftover.stack);
                    }
                }

                GiveCoins(ItemID.PlatinumCoin, plat);
                GiveCoins(ItemID.GoldCoin, gold);
                GiveCoins(ItemID.SilverCoin, silver);
                GiveCoins(ItemID.CopperCoin, copper);

                SoundEngine.PlaySound(SoundID.CoinPickup);
                string coinText = FormatCoins((int)totalSellPrice);
                CombatText.NewText(player.Hitbox, new Color(255, 240, 100), $"+{coinText} (已售卖 [i:{itemType}] x{stackCount})", false, false);
            }
            else
            {
                CombatText.NewText(player.Hitbox, new Color(180, 180, 180), $"已销毁 [i:{itemType}] x{stackCount}", false, false);
            }

            item.TurnToAir();
            return true;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            int count = GetStoredCount();
            int max = Capacity;

            if (count > 0)
            {
                string countText = count >= max ? $"已设定过滤样本: {count}/{max} (已满)" : $"已设定过滤样本: {count}/{max}";
                tooltips.Add(new TooltipLine(Mod, "TrashBagCount", countText)
                {
                    OverrideColor = Color.LightGreen
                });

                var stored = GetStoredItems();
                if (stored.Count > 20)
                {
                    string iconStream = string.Empty;
                    int lineIndex = 0;
                    for (int i = 0; i < stored.Count; i++)
                    {
                        var it = stored[i];
                        int showStack = it.stack > 99 ? 99 : it.stack;
                        iconStream += $"[i/s{showStack}:{it.type}]";
                        if ((i + 1) % 20 == 0)
                        {
                            tooltips.Add(new TooltipLine(Mod, $"TrashBagStream{++lineIndex}", iconStream));
                            iconStream = string.Empty;
                        }
                    }
                    if (!string.IsNullOrEmpty(iconStream))
                    {
                        tooltips.Add(new TooltipLine(Mod, $"TrashBagStream{++lineIndex}", iconStream));
                    }
                }
                else
                {
                    for (int i = 0; i < stored.Count; i++)
                    {
                        var it = stored[i];
                        string itemName = Lang.GetItemNameValue(it.type);
                        int singleSell = it.value / 5;
                        string sellText = singleSell > 0 ? $"[c/ffdd66:单价 {FormatCoins(singleSell)}]" : "[c/aaaaaa:无价值(自动销毁)]";
                        string line = $"[i/s{it.stack}:{it.type}] {itemName}  {sellText}";
                        tooltips.Add(new TooltipLine(Mod, $"TrashBagItem{i}", line)
                        {
                            OverrideColor = Color.LightSkyBlue
                        });
                    }
                }
            }
            else
            {
                tooltips.Add(new TooltipLine(Mod, "TrashBagEmpty", "垃圾桶内暂无过滤样本 (0/200)")
                {
                    OverrideColor = Color.SkyBlue
                });
            }
        }

        private static string FormatCoins(int value)
        {
            if (value <= 0) return "0 铜";
            int plat = value / 1000000;
            int gold = (value % 1000000) / 10000;
            int silver = (value % 10000) / 100;
            int copper = value % 100;

            string res = "";
            if (plat > 0) res += $"{plat} 铂 ";
            if (gold > 0) res += $"{gold} 金 ";
            if (silver > 0) res += $"{silver} 银 ";
            if (copper > 0 || string.IsNullOrEmpty(res)) res += $"{copper} 铜";
            return res.Trim();
        }

        public override void AddRecipes()
        {
            CreateRecipe(1)
                .AddIngredient(ItemID.TrashCan, 1)
                .AddIngredient(ItemID.Silk, 8)
                .AddTile(TileID.WorkBenches)
                .Register();
        }
    }
}