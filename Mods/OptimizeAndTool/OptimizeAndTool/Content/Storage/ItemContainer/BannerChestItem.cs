using System.Collections.Generic;
using Microsoft.Xna.Framework;
using OptimizeAndTool.Content.QoL;
using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ID;
using TPML.Content;
using TPML.Content.UI;

namespace OptimizeAndTool.Content.Storage.ItemContainer
{
    /// <summary>
    /// 旗帜盒 (BannerChest)
    /// 实体级独立容器，可容纳多达 500 种怪物旗帜，支持一键收集、存取、整理与自动拾取收纳；
    /// 存入的怪物旗帜常驻提供全套随身怪物旗帜增益，数据通过 TPML Sidecar 伴随存档无损保存。
    /// 作者: SaintCirno9
    /// </summary>
    public class BannerChestItem : ItemContainerItem
    {
        public override string Name => "BannerChest";
        public override string Texture => "BannerChest";
        public override int Capacity => 500;
        public override string ContainerTitle => "旗帜盒";

        public override void SetStaticDefaults()
        {
            ItemLoader.SetDisplayName(Type, "旗帜盒");
            ItemLoader.SetTooltip(Type, "便携式怪物旗帜收纳箱，可存储多达 500 格怪物旗帜\n[c/88ff88:【操作提示】] 物品栏右键或悬停中键打开收纳面板\n手持旗帜左键点击旗帜盒可直接存入\n若开启自动收纳，拾取怪物旗帜时将自动存入盒中\n存入的怪物旗帜将在随身范围内常驻提供怪物旗帜增益");
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
            return InfinitePotionAndBuff.ItemToBanner(item) >= 0;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            int count = GetStoredCount();
            int max = Capacity;

            if (count > 0)
            {
                string countText = count >= max ? $"已存入怪物旗帜: {count}/{max} (已满)" : $"已存入怪物旗帜: {count}/{max}";
                tooltips.Add(new TooltipLine(Mod, "BannerChestCount", countText)
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
                            tooltips.Add(new TooltipLine(Mod, $"BannerChestStream{++lineIndex}", iconStream));
                            iconStream = string.Empty;
                        }
                    }
                    if (!string.IsNullOrEmpty(iconStream))
                    {
                        tooltips.Add(new TooltipLine(Mod, $"BannerChestStream{++lineIndex}", iconStream));
                    }
                }
                else
                {
                    for (int i = 0; i < stored.Count; i++)
                    {
                        var it = stored[i];
                        string itemName = Lang.GetItemNameValue(it.type);
                        string line = $"[i/s{it.stack}:{it.type}] {itemName}  [c/88ff88:旗帜增益已生效]";
                        tooltips.Add(new TooltipLine(Mod, $"BannerChestItem{i}", line)
                        {
                            OverrideColor = Color.LightGreen
                        });
                    }
                }
            }
            else
            {
                tooltips.Add(new TooltipLine(Mod, "BannerChestEmpty", "旗帜盒内空无一物 (0/500)")
                {
                    OverrideColor = Color.SkyBlue
                });
            }
        }

        public override void AddRecipes()
        {
            CreateRecipe(1)
                .AddIngredient(ItemID.Wood, 8)
                .AddIngredient(ItemID.IronBar, 2)
                .AddTile(TileID.WorkBenches)
                .Register();

            CreateRecipe(1)
                .AddIngredient(ItemID.Wood, 8)
                .AddIngredient(ItemID.LeadBar, 2)
                .AddTile(TileID.WorkBenches)
                .Register();
        }
    }
}
