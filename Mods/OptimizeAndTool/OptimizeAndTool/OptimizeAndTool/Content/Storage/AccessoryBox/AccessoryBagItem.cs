using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ID;
using TPML.Content;
using TPML.Content.IO;
using TPML.Content.UI;

namespace OptimizeAndTool.Content.Storage.AccessoryBox
{
    /// <summary>
    /// 随身饰品袋实体物品 (AccessoryBag)
    /// 拥有独立 BagID 与伴随存档数据绑定，支持槽位饰品被动生效、外观显隐切换与工作台合成
    /// 作者: SaintCirno9
    /// </summary>
    public class AccessoryBagItem : ModItem
    {
        public override string Name => "AccessoryBag";
        public override string Texture => "AccessoryBag";

        public Item[] personalInventory;
        public bool[] hideVisuals;
        public Guid BagID = Guid.NewGuid();
        public string ShortID => BagID.ToString("N").Substring(0, 4).ToUpper();

        public event Action OnSlotsChanged;
        public void TriggerSlotsChanged() => OnSlotsChanged?.Invoke();

        public override void SetStaticDefaults()
        {
            ItemLoader.SetDisplayName(Type, "饰品袋");
            ItemLoader.SetTooltip(Type, "便携式饰品收纳与被动属性挂载袋\n[c/88ff88:【操作提示】] 物品栏右键或中键打开饰品面板\n每格右上角眼睛图标可切换饰品外观可见性\n光标悬停在饰品上按快捷键 (默认 ]) 可极速转移\n袋内饰品无需拿取可直接参与工作台合成配方");
            CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
        }

        public override void SetDefaults()
        {
            base.SetDefaults();
            int cap = Math.Max(10, Math.Min(150, AccessoryBagConfig.TotalSlots.val));
            personalInventory = new Item[cap];
            hideVisuals = new bool[cap];
            for (int i = 0; i < cap; i++)
            {
                personalInventory[i] = new Item();
                hideVisuals[i] = false;
            }

            if (Item != null)
            {
                Item.width = 28;
                Item.height = 28;
                Item.maxStack = 1;
                Item.rare = 2;
                Item.value = Item.sellPrice(0, 1, 0, 0);
                Item.consumable = false;
                Item.useStyle = ItemUseStyleID.None;
            }
        }

        public bool IsEmpty()
        {
            if (personalInventory == null) return true;
            for (int i = 0; i < personalInventory.Length; i++)
            {
                if (personalInventory[i] != null && !personalInventory[i].IsAir && personalInventory[i].stack > 0)
                    return false;
            }
            return true;
        }

        public void DropAllItems(Player player)
        {
            if (personalInventory == null || player == null) return;
            for (int i = 0; i < personalInventory.Length; i++)
            {
                if (personalInventory[i] != null && !personalInventory[i].IsAir)
                {
                    player.GetOrDropItem(personalInventory[i].Clone(), GetItemSettings.RefundConsumedItem);
                    personalInventory[i] = new Item();
                }
            }
            TriggerSlotsChanged();
        }

        public void ResetBagData()
        {
            BagID = Guid.NewGuid();
            int cap = Math.Max(10, Math.Min(150, AccessoryBagConfig.TotalSlots.val));
            personalInventory = new Item[cap];
            hideVisuals = new bool[cap];
            for (int i = 0; i < cap; i++)
            {
                personalInventory[i] = new Item();
                hideVisuals[i] = false;
            }
            TriggerSlotsChanged();
        }

        public int CountDuplicate(Item target)
        {
            if (target == null || target.IsAir || !target.accessory || personalInventory == null) return 0;
            int count = 0;
            for (int i = 0; i < personalInventory.Length; i++)
            {
                Item it = personalInventory[i];
                if (it != null && !it.IsAir && it.type == target.type)
                {
                    count += Math.Max(1, it.stack);
                }
            }
            return count;
        }

        public void ResizeToConfig()
        {
            int targetCap = Math.Max(10, Math.Min(150, AccessoryBagConfig.TotalSlots.val));
            if (personalInventory == null)
            {
                personalInventory = new Item[targetCap];
                hideVisuals = new bool[targetCap];
                for (int i = 0; i < targetCap; i++)
                {
                    personalInventory[i] = new Item();
                    hideVisuals[i] = false;
                }
                return;
            }

            if (personalInventory.Length == targetCap) return;

            Item[] oldInv = personalInventory;
            bool[] oldVis = hideVisuals;

            personalInventory = new Item[targetCap];
            hideVisuals = new bool[targetCap];
            for (int i = 0; i < targetCap; i++)
            {
                personalInventory[i] = new Item();
                hideVisuals[i] = false;
            }

            int keep = Math.Min(oldInv.Length, targetCap);
            for (int i = 0; i < keep; i++)
            {
                if (oldInv[i] != null && !oldInv[i].IsAir) personalInventory[i] = oldInv[i];
                if (oldVis != null && i < oldVis.Length) hideVisuals[i] = oldVis[i];
            }

            Player player = Main.LocalPlayer;
            if (player?.active == true && oldInv.Length > targetCap)
            {
                for (int i = targetCap; i < oldInv.Length; i++)
                {
                    if (oldInv[i] != null && !oldInv[i].IsAir)
                    {
                        player.GetOrDropItem(oldInv[i], GetItemSettings.RefundConsumedItem);
                    }
                }
            }

            TriggerSlotsChanged();
        }

        public override void SaveData(TagCompound tag)
        {
            tag["bagID"] = BagID.ToString();
            tag["totalSlots"] = personalInventory != null ? personalInventory.Length : 40;

            if (hideVisuals != null)
            {
                var visList = new List<bool>(hideVisuals);
                tag["hideVisuals"] = visList;
            }

            var list = new List<TagCompound>();
            if (personalInventory != null)
            {
                for (int i = 0; i < personalInventory.Length; i++)
                {
                    Item it = personalInventory[i];
                    if (it != null && !it.IsAir && it.stack > 0)
                    {
                        var entry = new TagCompound
                        {
                            ["slot"] = i,
                            ["id"] = it.type,
                            ["stack"] = it.stack,
                            ["prefix"] = it.prefix,
                            ["fav"] = it.favorited
                        };
                        if (it.type >= ItemID.Count)
                        {
                            ModItem modIt = ItemLoader.GetModItem(it.type);
                            if (modIt != null)
                            {
                                entry["mod"] = modIt.Mod?.Name ?? "TPML";
                                entry["name"] = modIt.Name;
                            }
                        }
                        list.Add(entry);
                    }
                }
            }
            tag["items"] = list;
        }

        public override void LoadData(TagCompound tag)
        {
            if (tag == null) return;

            if (tag.ContainsKey("bagID"))
            {
                if (Guid.TryParse(tag.GetString("bagID"), out Guid g)) BagID = g;
            }

            int cap = Math.Max(10, Math.Min(150, AccessoryBagConfig.TotalSlots.val));
            personalInventory = new Item[cap];
            hideVisuals = new bool[cap];
            for (int i = 0; i < cap; i++)
            {
                personalInventory[i] = new Item();
                hideVisuals[i] = false;
            }

            if (tag.ContainsKey("hideVisuals"))
            {
                try
                {
                    if (tag["hideVisuals"] is Newtonsoft.Json.Linq.JArray jArr)
                    {
                        int idx = 0;
                        foreach (var token in jArr)
                        {
                            if (idx < hideVisuals.Length) hideVisuals[idx++] = token.ToObject<bool>();
                        }
                    }
                    else if (tag["hideVisuals"] is List<bool> bList)
                    {
                        for (int i = 0; i < Math.Min(bList.Count, hideVisuals.Length); i++)
                        {
                            hideVisuals[i] = bList[i];
                        }
                    }
                }
                catch { }
            }

            if (tag.TryGetValue("items", out object obj))
            {
                if (obj is Newtonsoft.Json.Linq.JArray jArr)
                {
                    foreach (var token in jArr)
                    {
                        int slot = token["slot"]?.ToObject<int>() ?? -1;
                        int id = token["id"]?.ToObject<int>() ?? 0;
                        int stack = token["stack"]?.ToObject<int>() ?? 1;
                        int prefix = token["prefix"]?.ToObject<int>() ?? 0;
                        bool fav = token["fav"]?.ToObject<bool>() ?? false;
                        string mod = token["mod"]?.ToString();
                        string name = token["name"]?.ToString();

                        if (!string.IsNullOrEmpty(mod) && !string.IsNullOrEmpty(name))
                        {
                            int resolved = ItemLoader.ItemType(mod, name);
                            if (resolved > 0) id = resolved;
                        }

                        if (id > 0 && slot >= 0 && slot < cap)
                        {
                            Item it = new Item();
                            it.SetDefaults(id);
                            it.stack = Math.Max(1, Math.Min(stack, it.maxStack));
                            if (prefix > 0) it.Prefix(prefix);
                            it.favorited = fav;
                            personalInventory[slot] = it;
                        }
                    }
                }
                else if (obj is List<TagCompound> tagList)
                {
                    foreach (var itemTag in tagList)
                    {
                        int slot = itemTag.GetInt("slot");
                        int id = itemTag.GetInt("id");
                        int stack = itemTag.GetInt("stack");
                        int prefix = itemTag.GetInt("prefix");
                        bool fav = itemTag.GetBool("fav");
                        string mod = itemTag.GetString("mod");
                        string name = itemTag.GetString("name");

                        if (!string.IsNullOrEmpty(mod) && !string.IsNullOrEmpty(name))
                        {
                            int resolved = ItemLoader.ItemType(mod, name);
                            if (resolved > 0) id = resolved;
                        }

                        if (id > 0 && slot >= 0 && slot < cap)
                        {
                            Item it = new Item();
                            it.SetDefaults(id);
                            it.stack = Math.Max(1, Math.Min(stack, it.maxStack));
                            if (prefix > 0) it.Prefix(prefix);
                            it.favorited = fav;
                            personalInventory[slot] = it;
                        }
                    }
                }
            }

            TriggerSlotsChanged();
        }

        public override ModItem Clone(Item newEntity)
        {
            AccessoryBagItem clone = (AccessoryBagItem)base.Clone(newEntity);
            clone.BagID = BagID;
            int len = personalInventory != null ? personalInventory.Length : 40;
            clone.personalInventory = new Item[len];
            clone.hideVisuals = new bool[len];
            for (int i = 0; i < len; i++)
            {
                clone.personalInventory[i] = (personalInventory != null && i < personalInventory.Length && personalInventory[i] != null)
                    ? personalInventory[i].Clone() : new Item();
                clone.hideVisuals[i] = (hideVisuals != null && i < hideVisuals.Length) ? hideVisuals[i] : false;
            }
            return clone;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            if (personalInventory == null) return;

            var items = new List<Item>();
            for (int i = 0; i < personalInventory.Length; i++)
            {
                Item it = personalInventory[i];
                if (it != null && !it.IsAir && it.stack > 0)
                {
                    items.Add(it);
                }
            }

            int count = items.Count;
            int max = personalInventory.Length;

            if (count > 0)
            {
                string header = count >= max ? $"已存入饰品: {count}/{max} (已满)" : $"已存入饰品: {count}/{max}";
                tooltips.Add(new TooltipLine(Mod, "AccBagCount", header)
                {
                    OverrideColor = Color.LightGreen
                });

                int perRow = 8;
                string iconStream = string.Empty;
                int lineIndex = 0;
                for (int i = 0; i < Math.Min(count, 24); i++)
                {
                    Item it = items[i];
                    iconStream += $"[i:{it.type}]";
                    if ((i + 1) % perRow == 0)
                    {
                        lineIndex++;
                        tooltips.Add(new TooltipLine(Mod, $"AccBagStream{lineIndex}", iconStream));
                        iconStream = string.Empty;
                    }
                }
                if (!string.IsNullOrEmpty(iconStream))
                {
                    lineIndex++;
                    tooltips.Add(new TooltipLine(Mod, $"AccBagStream{lineIndex}", iconStream));
                }

                if (count > 24)
                {
                    tooltips.Add(new TooltipLine(Mod, "AccBagRemaining", $"还有 {count - 24} 个饰品...")
                    {
                        OverrideColor = Color.Gray
                    });
                }
            }
            else
            {
                tooltips.Add(new TooltipLine(Mod, "AccBagEmpty", $"饰品袋内空无一物 (0/{max})")
                {
                    OverrideColor = Color.SkyBlue
                });
            }

            tooltips.Add(new TooltipLine(Mod, "AccBagID", $"编号: [c/88ff88:{ShortID}]")
            {
                OverrideColor = Color.Gray
            });
        }

        public override void AddRecipes()
        {
            CreateRecipe(1)
                .AddIngredient(ItemID.Silk, 10)
                .AddIngredient(ItemID.IronBar, 10)
                .AddTile(TileID.Loom)
                .Register();

            CreateRecipe(1)
                .AddIngredient(ItemID.Silk, 10)
                .AddIngredient(ItemID.LeadBar, 10)
                .AddTile(TileID.Loom)
                .Register();

            CreateRecipe(1)
                .AddIngredient(ItemID.Silk, 10)
                .AddIngredient(ItemID.IronBar, 10)
                .AddTile(TileID.WorkBenches)
                .Register();

            CreateRecipe(1)
                .AddIngredient(ItemID.Silk, 10)
                .AddIngredient(ItemID.LeadBar, 10)
                .AddTile(TileID.WorkBenches)
                .Register();
        }
    }
}
