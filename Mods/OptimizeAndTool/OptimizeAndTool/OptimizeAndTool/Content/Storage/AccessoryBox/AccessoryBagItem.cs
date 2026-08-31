using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using OptimizeAndTool.Content.Storage.Core;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.Creative;
using Terraria.ID;
using TPML.Content;
using TPML.Content.IO;
using TPML.Content.UI;
using OptimizeAndTool.Content.Storage.ItemContainer;

namespace OptimizeAndTool.Content.Storage.AccessoryBox
{
    /// <summary>
    /// 随身饰品袋实体物品 (AccessoryBag)
    /// 拥有独立 BagID 与伴随存档数据绑定，实现通用 IBagInventory 契约，支持槽位饰品被动生效、外观显隐切换与工作台合成
    /// 作者: SaintCirno9
    /// </summary>
    public class AccessoryBagItem : ModItem, IBagInventory, IVisualToggleable, IToolbarCustomActions
    {
        public override string Name => "AccessoryBag";
        public override string Texture => "AccessoryBag";

        public Item[] personalInventory;
        public bool[] hideVisuals;
        public Guid BagID = Guid.NewGuid();
        public string ShortID => BagID.ToString("N").Substring(0, 4).ToUpper();

        public event Action OnSlotsChanged;
        public void TriggerSlotsChanged()
        {
            OnSlotsChanged?.Invoke();
            if (!Main.gameMenu && Main.netMode != 2)
            {
                Recipe.UpdateRecipeList();
            }
        }

        #region IBagInventory 实现
        public string Title => $"随身饰品袋 [{ShortID}]";
        public Item[] Slots => personalInventory;
        public int Capacity => personalInventory != null ? personalInventory.Length : 0;
        public bool CanFavorite => true;
        public bool ShowModSidebar => true;
        public bool ShowFilterBar => true;

        public static bool IsValidBagItem(Item item)
        {
            if (item == null || item.IsAir) return false;
            return item.accessory || item.headSlot >= 0 || item.bodySlot >= 0 || item.legSlot >= 0 || item.defense > 0 || item.prefix > 0;
        }

        public bool MeetEntryCriteria(Item item, int targetSlot = -1)
        {
            if (item == null || item.IsAir) return false;
            if (!IsValidBagItem(item)) return false;

            if (CheckDuplicates(item, targetSlot)) return false;
            return true;
        }

        public bool CheckDuplicates(Item candidate, int currentSlot)
        {
            if (candidate == null || candidate.IsAir || !IsValidBagItem(candidate)) return false;

            if (AccessoryBagConfig.PreventBagDuplicates.val && personalInventory != null)
            {
                for (int i = 0; i < personalInventory.Length; i++)
                {
                    if (i != currentSlot && personalInventory[i] != null && !personalInventory[i].IsAir && personalInventory[i].type == candidate.type)
                    {
                        SoundEngine.PlaySound(SoundID.MenuClose);
                        Main.NewText($"[饰品袋] 已存在同种物品 {candidate.Name}，禁止重复存放！", Color.OrangeRed);
                        return true;
                    }
                }
            }

            if (AccessoryBagConfig.PreventPlayerBagDuplicates.val && Main.LocalPlayer?.armor != null)
            {
                for (int i = 0; i < Main.LocalPlayer.armor.Length; i++)
                {
                    Item armorIt = Main.LocalPlayer.armor[i];
                    if (armorIt != null && !armorIt.IsAir && armorIt.type == candidate.type)
                    {
                        SoundEngine.PlaySound(SoundID.MenuClose);
                        Main.NewText($"[饰品袋] 角色已装备 {candidate.Name}，禁止在袋中重复挂载！", Color.OrangeRed);
                        return true;
                    }
                }
            }

            if (AccessoryBagConfig.EnableMaxDuplicateAccessory.val)
            {
                int curCount = CountDuplicate(candidate);
                if (curCount >= AccessoryBagConfig.MaxDuplicateAccessory.val)
                {
                    SoundEngine.PlaySound(SoundID.MenuClose);
                    Main.NewText($"[饰品袋] 同种物品最大上限为 {AccessoryBagConfig.MaxDuplicateAccessory.val} 个！", Color.OrangeRed);
                    return true;
                }
            }

            return false;
        }

        public bool TryDeposit(Item item, bool sort = true)
        {
            if (item == null || item.IsAir || !IsValidBagItem(item) || personalInventory == null) return false;
            if (CheckDuplicates(item, -1)) return false;

            // 1. 同类堆叠
            for (int i = 0; i < personalInventory.Length; i++)
            {
                Item target = personalInventory[i];
                if (target != null && !target.IsAir && target.type == item.type && target.stack < target.maxStack && Item.CanStack(target, item))
                {
                    int take = Math.Min(item.stack, target.maxStack - target.stack);
                    target.stack += take;
                    item.stack -= take;
                    if (item.stack <= 0)
                    {
                        item.TurnToAir();
                        TriggerSlotsChanged();
                        return true;
                    }
                }
            }

            // 2. 放入空格
            for (int i = 0; i < personalInventory.Length; i++)
            {
                Item target = personalInventory[i];
                if (target == null || target.IsAir)
                {
                    personalInventory[i] = item.Clone();
                    item.TurnToAir();
                    TriggerSlotsChanged();
                    return true;
                }
            }

            return false;
        }

        public bool TryDepositFromSlot(Item[] inv, int slot, bool justCheck)
        {
            if (inv == null || slot < 0 || slot >= inv.Length) return false;
            Item item = inv[slot];
            if (item == null || item.IsAir || item.favorited || !IsValidBagItem(item) || personalInventory == null) return false;

            if (justCheck)
            {
                if (CheckDuplicates(item, -1)) return false;
                for (int i = 0; i < personalInventory.Length; i++)
                {
                    Item target = personalInventory[i];
                    if (target == null || target.IsAir) return true;
                    if (target.type == item.type && target.stack < target.maxStack && Item.CanStack(target, item)) return true;
                }
                return false;
            }

            bool res = TryDeposit(item, sort: true);
            if (item.stack <= 0) inv[slot] = new Item();
            return res;
        }

        public void DepositAll(Player player)
        {
            if (player?.inventory == null || personalInventory == null) return;

            bool moved = false;
            Item[] pInv = player.inventory;
            Item[] bInv = personalInventory;

            for (int i = 10; i < 50; i++)
            {
                Item pIt = pInv[i];
                if (pIt == null || pIt.IsAir || pIt.favorited || !IsValidBagItem(pIt)) continue;
                if (CheckDuplicates(pIt, -1)) continue;

                for (int j = 0; j < bInv.Length; j++)
                {
                    if (bInv[j] == null || bInv[j].IsAir)
                    {
                        bInv[j] = pIt.Clone();
                        pInv[i] = new Item();
                        moved = true;
                        break;
                    }
                }
            }

            if (moved)
            {
                SoundEngine.PlaySound(SoundID.Grab);
                TriggerSlotsChanged();
            }
        }

        public void QuickStack(Player player)
        {
            if (player?.inventory == null || personalInventory == null) return;

            bool moved = false;
            Item[] pInv = player.inventory;
            Item[] bInv = personalInventory;

            for (int i = 10; i < 50; i++)
            {
                Item pIt = pInv[i];
                if (pIt == null || pIt.IsAir || pIt.favorited || !IsValidBagItem(pIt)) continue;

                for (int j = 0; j < bInv.Length; j++)
                {
                    Item bIt = bInv[j];
                    if (bIt != null && !bIt.IsAir && bIt.type == pIt.type && bIt.stack < bIt.maxStack && Item.CanStack(bIt, pIt))
                    {
                        int take = Math.Min(pIt.stack, bIt.maxStack - bIt.stack);
                        bIt.stack += take;
                        pIt.stack -= take;
                        moved = true;
                        if (pIt.stack <= 0)
                        {
                            pInv[i] = new Item();
                            break;
                        }
                    }
                }
            }

            if (moved)
            {
                SoundEngine.PlaySound(SoundID.Grab);
                TriggerSlotsChanged();
            }
        }

        public void LootAll(Player player, Func<Item, bool> filter = null)
        {
            if (player?.inventory == null || personalInventory == null) return;

            bool moved = false;
            Item[] bInv = personalInventory;

            try
            {
                ItemContainerItem.IsTransferringOut = true;
                for (int i = 0; i < bInv.Length; i++)
                {
                    Item bIt = bInv[i];
                    if (bIt == null || bIt.IsAir) continue;
                    if (filter != null && !filter(bIt)) continue;

                    int orig = bIt.stack;
                    bInv[i] = player.GetItem(bIt, GetItemSettings.QuickTransferFromSlot);
                    if (bInv[i] == null) bInv[i] = new Item();
                    if (bInv[i].stack != orig) moved = true;
                }
            }
            finally
            {
                ItemContainerItem.IsTransferringOut = false;
            }

            if (moved)
            {
                SoundEngine.PlaySound(SoundID.Grab);
                TriggerSlotsChanged();
            }
        }

        public void Sort()
        {
            if (personalInventory == null) return;
            Item[] bInv = personalInventory;

            var items = new List<Item>();
            var favPositions = new Dictionary<int, Item>();

            for (int i = 0; i < bInv.Length; i++)
            {
                if (bInv[i] != null && !bInv[i].IsAir)
                {
                    if (bInv[i].favorited) favPositions[i] = bInv[i];
                    else items.Add(bInv[i]);
                }
            }

            items.Sort((x, y) =>
            {
                if (x.rare != y.rare) return y.rare.CompareTo(x.rare);
                if (x.value != y.value) return y.value.CompareTo(x.value);
                return x.type.CompareTo(y.type);
            });

            int listIdx = 0;
            for (int i = 0; i < bInv.Length; i++)
            {
                if (favPositions.ContainsKey(i))
                {
                    bInv[i] = favPositions[i];
                }
                else if (listIdx < items.Count)
                {
                    bInv[i] = items[listIdx++];
                }
                else
                {
                    bInv[i] = new Item();
                }
            }

            SoundEngine.PlaySound(SoundID.Grab);
            TriggerSlotsChanged();
        }

        public string GetCapacityText()
        {
            if (personalInventory == null) return "0/0";
            int filled = 0;
            for (int i = 0; i < personalInventory.Length; i++)
            {
                if (personalInventory[i] != null && !personalInventory[i].IsAir) filled++;
            }
            string t = $"已存: {filled}/{personalInventory.Length}";
            if (AccessoryBagConfig.EnableEffectiveSlotsLimit.val)
            {
                t += $" (生效前 {AccessoryBagConfig.EffectiveSlots.val} 格)";
            }
            return t;
        }

        public bool IsDynamicCapacity => false;
        public void EnsureTrailingEmptySlots(int trailingCount = 10) { }
        public void ExpandCapacity(int addedCount) { }
        #endregion

        #region IVisualToggleable 实现
        public bool[] HideVisuals => hideVisuals;

        public void ToggleVisual(int slot)
        {
            if (hideVisuals != null && slot >= 0 && slot < hideVisuals.Length)
            {
                hideVisuals[slot] = !hideVisuals[slot];
                TriggerSlotsChanged();
            }
        }

        public bool HasAnyVisibleVisuals()
        {
            if (hideVisuals == null) return false;
            for (int i = 0; i < hideVisuals.Length; i++)
            {
                if (!hideVisuals[i]) return true;
            }
            return false;
        }

        public void ToggleAllVisuals()
        {
            if (hideVisuals == null) return;
            bool targetHidden = HasAnyVisibleVisuals();
            for (int i = 0; i < hideVisuals.Length; i++)
            {
                hideVisuals[i] = targetHidden;
            }
            TriggerSlotsChanged();
        }
        #endregion

        #region IToolbarCustomActions 实现
        public IEnumerable<BagToolbarButton> GetCustomToolbarButtons()
        {
            yield return new BagToolbarButton(
                () => AccessoryBagConfig.EnablePassive.val ? "被动饰品属性: 已生效 (点击禁用)" : "被动饰品属性: 已禁用 (点击开启)",
                () => AccessoryBagConfig.EnablePassive.val ? "Images/Item_158" : "Images/UI/InfoIcon_5",
                () =>
                {
                    AccessoryBagConfig.EnablePassive.val = !AccessoryBagConfig.EnablePassive.val;
                    SoundEngine.PlaySound(SoundID.MenuTick);
                    TriggerSlotsChanged();
                }
            );
        }
        #endregion

        public override void SetStaticDefaults()
        {
            ItemLoader.SetDisplayName(Type, "随身饰品袋");
            ItemLoader.SetTooltip(Type, "便携式饰品与装备收纳与被动属性挂载袋\n[c/88ff88:【核心特性】] 支持饰品与装备（头盔/胸甲/护腿），自动激活属性与套装奖励\n[c/88ff88:【操作提示】] 物品栏右键或中键打开面板，每格右上角眼睛可切换外观\n光标悬停在饰品/装备上按快捷键 (默认 ]) 可极速转移，袋内物品直连参与配方合成");
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
            if (target == null || target.IsAir || !IsValidBagItem(target) || personalInventory == null) return 0;
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

            int savedSlots = 0;
            if (tag.ContainsKey("totalSlots"))
            {
                try { savedSlots = tag.GetInt("totalSlots"); } catch { }
            }
            int cap = Math.Max(10, Math.Min(150, AccessoryBagConfig.TotalSlots.val));
            if (savedSlots > cap) cap = Math.Min(150, savedSlots);
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
                string header = count >= max ? $"已存入物品: {count}/{max} (已满)" : $"已存入物品: {count}/{max}";
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
                    tooltips.Add(new TooltipLine(Mod, "AccBagRemaining", $"还有 {count - 24} 个物品...")
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
