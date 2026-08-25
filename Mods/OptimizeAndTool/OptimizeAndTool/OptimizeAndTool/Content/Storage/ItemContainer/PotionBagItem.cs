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
    /// 药水袋 (PotionBag)
    /// 实体级独立容器，可容纳多达 200 种药水，支持一键收集、存取、整理与自动拾取收纳；
    /// 存入的药水自动计入无尽药水续杯检测，数据通过 TPML Sidecar 伴随存档无损保存。
    /// 作者: SaintCirno9
    /// </summary>
    public class PotionBagItem : ItemContainerItem
    {
        public override string Name => "PotionBag";
        public override string Texture => "PotionBag";
        public override int Capacity => 200;
        public override string ContainerTitle => "药水袋";

        public override void SetStaticDefaults()
        {
            ItemLoader.SetDisplayName(Type, "药水袋");
            ItemLoader.SetTooltip(Type, "便携式药水收纳袋，可存储多达 200 格药水\n[c/88ff88:【操作提示】] 物品栏右键或悬停中键打开收纳面板\n手持药水左键点击药水袋可直接存入\n若开启自动收纳，拾取药水时将自动存入袋中\n存入的药水自动参与无尽药水续杯判定");
            CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
        }

        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.width = 48;
            Item.height = 42;
            Item.maxStack = 1;
            Item.rare = 10;
            Item.value = Item.sellPrice(0, 0, 30, 0);
            Item.consumable = false;
            Item.useStyle = ItemUseStyleID.None;
        }

        public override bool MeetEntryCriteria(Item item)
        {
            if (item == null || item.IsAir || item.type <= 0) return false;
            if (item.buffType > 0)
            {
                return item.consumable;
            }
            return false;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            int count = GetStoredCount();
            int max = Capacity;

            if (count > 0)
            {
                string countText = count >= max ? $"已存入药水: {count}/{max} (已满)" : $"已存入药水: {count}/{max}";
                tooltips.Add(new TooltipLine(Mod, "PotionBagCount", countText)
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
                            tooltips.Add(new TooltipLine(Mod, $"PotionBagStream{++lineIndex}", iconStream));
                            iconStream = string.Empty;
                        }
                    }
                    if (!string.IsNullOrEmpty(iconStream))
                    {
                        tooltips.Add(new TooltipLine(Mod, $"PotionBagStream{++lineIndex}", iconStream));
                    }
                }
                else
                {
                    int threshold = InfinitePotionAndBuff.PotionThreshold.val;
                    if (threshold <= 0) threshold = 30;

                    for (int i = 0; i < stored.Count; i++)
                    {
                        var it = stored[i];
                        bool active = it.stack >= threshold;
                        string itemName = Lang.GetItemNameValue(it.type);
                        string statusText = active ? "[c/88ff88:无尽生效]" : $"[c/88ccff:未达阈值 ({it.stack}/{threshold})]";
                        string line = $"[i/s{it.stack}:{it.type}] {itemName}  {statusText}";
                        tooltips.Add(new TooltipLine(Mod, $"PotionBagItem{i}", line)
                        {
                            OverrideColor = active ? Color.LightGreen : Color.SkyBlue
                        });
                    }
                }
            }
            else
            {
                tooltips.Add(new TooltipLine(Mod, "PotionBagEmpty", "药水袋内空无一物 (0/200)")
                {
                    OverrideColor = Color.SkyBlue
                });
            }
        }

        public override void AddRecipes()
        {
            CreateRecipe(1)
                .AddIngredient(ItemID.Silk, 8)
                .AddTile(TileID.Loom)
                .Register();
        }
    }
}
