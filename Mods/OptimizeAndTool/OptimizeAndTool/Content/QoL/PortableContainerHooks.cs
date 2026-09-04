using CommandHelp;
using BigBagMod = OptimizeAndTool.Content.BigBag.BigBag;
using OptimizeAndTool.Content.BigBag;
using OptimizeAndTool.Utils;
using OptimizeAndTool.Utils.quickBuild;
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.Audio;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.UI;
using Terraria.UI.Gamepad;
using TPML.Core.Diagnostics;

namespace OptimizeAndTool.Content.QoL
{
    /// <summary>
    /// 随身容器增强门控（基于 HookGen 强类型 On_ 门控）：
    /// 1. 存钱罐/保险箱/护卫熔炉/虚空袋中的材料自动参与制作系统（可用判定与消耗扣除）；
    /// 2. 物品栏中键悬停存钱罐/钱币槽/切斯特/保险箱/护卫熔炉/虚空袋/虚空库/虚空眼等物品直接打开对应随身容器；再次中键可快速关闭；
    /// 3. 对照 ImproveGame：Hook Player.HandleBeingInChestRange 避免随身容器因未放置实体图格而在下一帧被原版关闭；
    /// 4. Hook Main.MouseText_DrawItemTooltip_GetLinesInfo 完美注入原生物品 Tooltip 快捷键提示。
    /// 原版私有成员（由 Publicizer 强类型直连）：Recipe._recipeChests、Recipe.CollectItems、Player.HandleBeingInChestRange。
    /// 作者: SaintCirno9
    /// </summary>
    public static class PortableContainerHooks
    {
        public static GetSetReset<bool> EnableContainerCraft = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> EnableMiddleClickOpen = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> EnableAutoCoinsToPiggyBank = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> RequirePiggyBankItem = new GetSetReset<bool>(true, true);
        /// 便携容器物品 -> 对应的随身 Chest 界面编号（-2 存钱罐 / -3 保险箱 / -4 护卫熔炉 / -5 虚空库）
        /// 全面对齐 ImproveGame (Lookups.BankItems) 支持所有便携媒介
        /// </summary>
        public static readonly Dictionary<int, int> OpenChestMap = new Dictionary<int, int>
        {
            // Bank2: 存钱罐系列 (-2)
            { ItemID.PiggyBank, -2 },
            { ItemID.MoneyTrough, -2 },
            { ItemID.ChesterPetItem, -2 },

            // Bank3: 保险箱系列 (-3)
            { ItemID.Safe, -3 },

            // Bank4: 护卫熔炉系列 (-4)
            { ItemID.DefendersForge, -4 },

            // Bank5: 虚空库系列 (-5)
            { ItemID.VoidVault, -5 },
            { ItemID.VoidLens, -5 },
            { ItemID.ClosedVoidBag, -5 }
        };

        public static List<CommandObject> GetCO()
        {
            return new List<CommandObject>
            {
                CommandBuild.get2("portableContainerCraft", EnableContainerCraft),
                CommandBuild.get2("portableContainerMiddleClick", EnableMiddleClickOpen),
                CommandBuild.get2("autoCoinsToPiggyBank", EnableAutoCoinsToPiggyBank),
                CommandBuild.get2("autoCoinsRequireBankItem", RequirePiggyBankItem)
            };
        }

        public static List<UIElement> GetUI()
        {
            return new List<UIElement>
            {
                UIBuild.get2(EnableContainerCraft, "存钱罐、保险箱、护卫熔炉、虚空袋中的材料自动参与制作判定与消耗扣除", "Images/Item_346", "随身容器材料制作"),
                UIBuild.get2(EnableMiddleClickOpen, "物品栏中鼠标悬停存钱罐、钱币槽、切斯特、保险箱、护卫熔炉、虚空袋等物品时按中键直接打开/关闭对应容器", "Images/Item_87", "中键快捷打开容器"),
                UIBuild.get2(EnableAutoCoinsToPiggyBank, "拾取的钱币（铜/银/金/铂金）自动存入随身猪猪存钱罐并自动进位合并（不占用背包格子）", "Images/Item_87", "钱币自动存入存钱罐"),
                UIBuild.get2(RequirePiggyBankItem, "仅当背包内持有存钱罐、钱币槽或眼球储钱罐（切斯特）时才自动存入存钱罐", "Images/Item_3213", "需持有存钱罐物品")
            };
        }

        public static void AddBankToRecipe(Chest bank)
        {
            if (bank?.item == null) return;

            // 原版虚空袋启用时已加入 bank4，或当前已处于某随身容器界面，重复加入会导致材料双计数
            if (Recipe._recipeChests != null && !Recipe._recipeChests.Contains(bank))
            {
                Recipe._recipeChests.Add(bank);
                Recipe.CollectItems(bank.item, bank.maxItems);
            }
        }

        /// <summary>
        /// 开关随身容器（Toggle），带专属音效与配方刷新
        /// </summary>
        public static void ToggleChest(Player player, int chestId, int itemType)
        {
            if (player == null) return;

            if (player.chest == chestId)
            {
                // 若已处于当前随身容器，中键直接关闭
                player.chest = -1;
                if (itemType == ItemID.ChesterPetItem) SoundEngine.PlaySound(SoundID.ChesterClose);
                else SoundEngine.PlaySound(SoundID.MenuClose);
                Recipe.UpdateRecipeList();
            }
            else
            {
                // 打开目标随身容器
                player.chest = chestId;
                for (int i = 0; i < 40; i++)
                {
                    ItemSlot.SetGlow(i, -1f, true);
                }
                player.chestX = (int)player.Center.X / 16;
                player.chestY = (int)player.Center.Y / 16;
                player.SetTalkNPC(-1);
                Main.SetNPCShopIndex(0);
                Main.playerInventory = true;
                UILinkPointNavigator.ForceMovementCooldown(120);

                // 播放对应容器的原生开箱音效
                if (itemType == ItemID.ChesterPetItem) SoundEngine.PlaySound(SoundID.ChesterOpen);
                else if (itemType == ItemID.MoneyTrough) SoundEngine.PlaySound(SoundID.Item59);
                else if (itemType == ItemID.VoidVault || itemType == ItemID.ClosedVoidBag || itemType == ItemID.VoidLens) SoundEngine.PlaySound(SoundID.Item130);
                else SoundEngine.PlaySound(SoundID.MenuOpen);

                Recipe.UpdateRecipeList();
            }
        }
        /// <summary>
        /// 将玩家背包（0~57格，包含50~53专用钱币槽与普通背包中未收藏的钱币）
        /// 在入包后自动汇聚转移到随身猪猪存钱罐（player.bank）中，并自动执行100进制进位与整理。
        /// 这样既完整保留原版获得钱币时的原生屏幕浮字提示（PopupText）与音效，又能让背包随时保持无钱币负担！
        /// </summary>
        public static bool TransferInventoryCoinsToPiggyBank(Player player)
        {
            if (player?.bank?.item == null || player.inventory == null)
                return false;

            if (!EnableAutoCoinsToPiggyBank.val)
                return false;

            if (RequirePiggyBankItem.val)
            {
                bool hasBankItem = player.HasItem(ItemID.PiggyBank) ||
                                   player.HasItem(ItemID.MoneyTrough) ||
                                   player.HasItem(ItemID.ChesterPetItem);
                if (!hasBankItem) return false;
            }

            Item[] pInv = player.inventory;
            Item[] bankInv = player.bank.item;

            // 1. 扫描背包中所有未收藏的钱币
            List<int> coinSlotsInInv = new List<int>();
            long incomingCoinValue = 0;
            for (int i = 0; i < pInv.Length; i++)
            {
                Item it = pInv[i];
                if (it != null && it.stack > 0 && it.IsACoin && !it.favorited)
                {
                    long val = 0;
                    switch (it.type)
                    {
                        case ItemID.CopperCoin: val = it.stack; break;
                        case ItemID.SilverCoin: val = (long)it.stack * 100L; break;
                        case ItemID.GoldCoin: val = (long)it.stack * 10000L; break;
                        case ItemID.PlatinumCoin: val = (long)it.stack * 1000000L; break;
                    }
                    if (val > 0)
                    {
                        incomingCoinValue += val;
                        coinSlotsInInv.Add(i);
                    }
                }
            }

            if (incomingCoinValue <= 0 || coinSlotsInInv.Count == 0)
                return false;

            // 2. 统计存钱罐中现有的钱币总价值并计算合并总值
            long currentBankValue = Terraria.Utils.CoinsCount(out bool overflow, bankInv);
            long totalValue = currentBankValue + incomingCoinValue;

            // 3. 记录存钱罐中原本存放钱币的格子（按类型映射或槽位列表）
            Dictionary<int, int> coinTypeToSlot = new Dictionary<int, int>();
            List<int> existingCoinSlots = new List<int>();
            for (int i = 0; i < bankInv.Length; i++)
            {
                Item it = bankInv[i];
                if (it != null && it.stack > 0 && it.IsACoin)
                {
                    if (!coinTypeToSlot.ContainsKey(it.type))
                    {
                        coinTypeToSlot[it.type] = i;
                    }
                    existingCoinSlots.Add(i);
                }
            }

            // 4. 计算重新拆分进位后的 4 种钱币数量 [0:铜, 1:银, 2:金, 3:铂金]
            int[] split = Terraria.Utils.CoinsSplit(totalValue);
            int[] coinTypes = new int[] { ItemID.CopperCoin, ItemID.SilverCoin, ItemID.GoldCoin, ItemID.PlatinumCoin };

            int slotsNeeded = 0;
            for (int i = 0; i < 4; i++)
            {
                if (split[i] > 0)
                {
                    // 原版钱币 maxStack 恒为 100（Item.cs case 71-74），且 ItemMaxStackPatch 明确排除钱币，
                    // 不能用 9999 计算槽位，否则 ≥101 铂金会写入非法堆叠（101/100）。
                    int maxStack = 100;
                    slotsNeeded += (int)Math.Ceiling((double)split[i] / maxStack);
                }
            }

            List<int> emptySlots = new List<int>();
            for (int i = 0; i < bankInv.Length; i++)
            {
                if (bankInv[i] == null || bankInv[i].IsAir || bankInv[i].stack <= 0)
                {
                    emptySlots.Add(i);
                }
            }

            int totalAvailableSlots = existingCoinSlots.Count + emptySlots.Count;
            if (slotsNeeded > totalAvailableSlots)
            {
                // 存钱罐空间不足以容纳进位拆分后的钱币，保留在普通背包中
                return false;
            }

            // 5. 先清空存钱罐原本所有钱币格
            foreach (int slot in existingCoinSlots)
            {
                bankInv[slot] = new Item();
                if (!emptySlots.Contains(slot))
                {
                    emptySlots.Insert(0, slot); // 优先复用原钱币格
                }
            }

            // 6. 依次填入进位后的钱币（从铜币到铂金币，优先放回原本的槽位）
            for (int i = 0; i < 4; i++)
            {
                int type = coinTypes[i];
                int count = split[i];
                if (count <= 0) continue;

                while (count > 0)
                {
                    int stackToPut = Math.Min(count, 100); // 钱币 maxStack 恒 100，避免非法堆叠
                    count -= stackToPut;

                    int targetSlot = -1;
                    if (coinTypeToSlot.TryGetValue(type, out int oldSlot) && (bankInv[oldSlot] == null || bankInv[oldSlot].IsAir))
                    {
                        targetSlot = oldSlot;
                        emptySlots.Remove(oldSlot);
                    }
                    else if (emptySlots.Count > 0)
                    {
                        targetSlot = emptySlots[0];
                        emptySlots.RemoveAt(0);
                    }

                    if (targetSlot >= 0 && targetSlot < bankInv.Length)
                    {
                        bankInv[targetSlot] = new Item();
                        bankInv[targetSlot].SetDefaults(type);
                        bankInv[targetSlot].stack = stackToPut;
                    }
                }
            }

            // 7. 清空背包中已被成功转移到存钱罐的钱币槽位
            foreach (int invSlot in coinSlotsInInv)
            {
                pInv[invSlot].TurnToAir();
            }

            // 若当前正在查看存钱罐界面，刷新配方与界面状态
            if (player.chest == -2)
            {
                Recipe.UpdateRecipeList();
            }

            return true;
        }

        private static bool _registered = false;

        public static void RegisterAll()
        {
            if (_registered) return;
            On_Recipe.CollectItemsFromChests += Hook_CollectItemsFromChests;
            On_Player.HandleBeingInChestRange += Hook_HandleBeingInChestRange;
            On_ItemSlot.Handle_ItemArray_int_int_bool += Hook_ItemSlotHandle;
            On_Main.MouseText_DrawItemTooltip_GetLinesInfo += Hook_MouseText_DrawItemTooltip_GetLinesInfo;
            On_Player.GetItem += Hook_GetItem;
            On_Player.DoCoins += Hook_DoCoins;
            On_NPC.SpawnAllowed_Merchant += Hook_SpawnAllowed_Merchant;
            _registered = true;
        }

        public static void UnregisterAll()
        {
            if (!_registered) return;
            On_Recipe.CollectItemsFromChests -= Hook_CollectItemsFromChests;
            On_Player.HandleBeingInChestRange -= Hook_HandleBeingInChestRange;
            On_ItemSlot.Handle_ItemArray_int_int_bool -= Hook_ItemSlotHandle;
            On_Main.MouseText_DrawItemTooltip_GetLinesInfo -= Hook_MouseText_DrawItemTooltip_GetLinesInfo;
            On_Player.GetItem -= Hook_GetItem;
            On_Player.DoCoins -= Hook_DoCoins;
            On_NPC.SpawnAllowed_Merchant -= Hook_SpawnAllowed_Merchant;
            _registered = false;
        }

        private static void Hook_CollectItemsFromChests(On_Recipe.orig_CollectItemsFromChests orig, Player player)
        {
            orig(player);

            if (player == null) return;

            using (PerformanceProfiler.Measure("OptimizeAndTool", "PortableContainer.CraftCollection"))
            {
                if (EnableContainerCraft.val)
                {
                    AddBankToRecipe(player.bank);
                    AddBankToRecipe(player.bank2);
                    AddBankToRecipe(player.bank3);
                    AddBankToRecipe(player.bank4);
                }
            }
        }

        private static void Hook_HandleBeingInChestRange(On_Player.orig_HandleBeingInChestRange orig, Player self)
        {
            if (self == null) return;

            // 当开启中键打开容器，且当前正处于随身便携容器（-2 存钱罐 / -3 保险箱 / -4 护卫熔炉 / -5 虚空库）时，
            // 阻断原版对世界实体图格的强行距离校验，防止下一帧被立即关闭！
            if (EnableMiddleClickOpen.val && self.chest < -1)
            {
                return;
            }

            orig(self);
        }

        private static void Hook_ItemSlotHandle(On_ItemSlot.orig_Handle_ItemArray_int_int_bool orig, Item[] inv, int context, int slot, bool allowInteract)
        {
            orig(inv, context, slot, allowInteract);

            if (!EnableMiddleClickOpen.val) return;
            if (!Main.playerInventory || !Main.mouseItem.IsAir) return;
            if (Main.npcShop > 0) return;
            if (inv == null || slot < 0 || slot >= inv.Length) return;

            Item item = inv[slot];
            if (item == null || item.IsAir) return;
            if (!OpenChestMap.TryGetValue(item.type, out int chestId)) return;

            // 仅在背包主区域、快捷栏、钱币栏、弹药栏、随身容器内部等有效槽位响应中键
            if (context != ItemSlot.Context.InventoryItem &&
                context != ItemSlot.Context.InventoryCoin &&
                context != ItemSlot.Context.InventoryAmmo &&
                context != ItemSlot.Context.HotbarItem &&
                context != ItemSlot.Context.BankItem &&
                context != ItemSlot.Context.ChestItem)
            {
                return;
            }

            // 鼠标中键按下边沿检测（防止按住中键连续开闭闪烁）
            if (PlayerInput.MouseInfo.MiddleButton == ButtonState.Pressed &&
                PlayerInput.MouseInfoOld.MiddleButton == ButtonState.Released)
            {
                // 立即消费本次中键事件，防止同一帧内被重复触发
                PlayerInput.MouseInfoOld = PlayerInput.MouseInfo;
                ToggleChest(Main.LocalPlayer, chestId, item.type);
            }
        }

        private static void Hook_MouseText_DrawItemTooltip_GetLinesInfo(
            On_Main.orig_MouseText_DrawItemTooltip_GetLinesInfo orig,
            Item item,
            ref int yoyoLogo,
            float oldKB,
            ref int numLines,
            string[] toolTipLine,
            Color[] lineColors)
        {
            orig(item, ref yoyoLogo, oldKB, ref numLines, toolTipLine, lineColors);

            if (!EnableMiddleClickOpen.val) return;
            if (item == null || item.IsAir) return;
            if (!OpenChestMap.TryGetValue(item.type, out int chestId)) return;

            // 防重检查
            for (int i = 0; i < numLines; i++)
            {
                if (toolTipLine[i] != null && toolTipLine[i].StartsWith("[鼠标中键]"))
                {
                    return;
                }
            }

            if (numLines < toolTipLine.Length - 1)
            {
                string actionText = (Main.LocalPlayer.chest == chestId) ? "关闭" : "打开";
                toolTipLine[numLines] = $"[鼠标中键] {actionText}随身容器";
                lineColors[numLines] = new Color(112, 219, 147); // 浅绿色
                numLines++;
            }
        }

        private static Item Hook_GetItem(On_Player.orig_GetItem orig, Player self, Item newItem, GetItemSettings settings)
        {
            Item result = orig(self, newItem, settings);

            if (self == null || newItem == null || !newItem.IsACoin)
                return result;

            // 等钱币进入背包并触发完原版 PopupText 提示后，自动转移到存钱罐
            TransferInventoryCoinsToPiggyBank(self);
            return result;
        }

        private static void Hook_DoCoins(On_Player.orig_DoCoins orig, Player self, int i)
        {
            orig(self, i);

            if (self == null || self.whoAmI != Main.myPlayer) return;
            TransferInventoryCoinsToPiggyBank(self);
        }

        private static bool Hook_SpawnAllowed_Merchant(On_NPC.orig_SpawnAllowed_Merchant orig)
        {
            bool result = orig();
            if (result) return true;
            if (NPC.unlockedMerchantSpawn) return true;

            long totalCoins = 0;
            const long targetCoins = 5000L; // 50 银币 = 5000 铜币

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (player == null || !player.active) continue;

                // 累加背包（0~57格）
                totalCoins += CountCoinsInItems(player.inventory);
                if (totalCoins >= targetCoins)
                {
                    return true;
                }

                // 累加四大随身银行
                if (player.bank?.item != null)
                {
                    totalCoins += CountCoinsInItems(player.bank.item);
                    if (totalCoins >= targetCoins)
                    {
                        return true;
                    }
                }
                if (player.bank2?.item != null)
                {
                    totalCoins += CountCoinsInItems(player.bank2.item);
                    if (totalCoins >= targetCoins)
                    {
                        return true;
                    }
                }
                if (player.bank3?.item != null)
                {
                    totalCoins += CountCoinsInItems(player.bank3.item);
                    if (totalCoins >= targetCoins)
                    {
                        return true;
                    }
                }
                if (player.bank4?.item != null)
                {
                    totalCoins += CountCoinsInItems(player.bank4.item);
                    if (totalCoins >= targetCoins)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static long CountCoinsInItems(Item[] items)
        {
            if (items == null) return 0;
            long val = 0;
            for (int j = 0; j < items.Length; j++)
            {
                Item item = items[j];
                if (item != null && item.stack > 0 && item.IsACoin)
                {
                    switch (item.type)
                    {
                        case ItemID.CopperCoin: val += item.stack; break;
                        case ItemID.SilverCoin: val += (long)item.stack * 100L; break;
                        case ItemID.GoldCoin: val += (long)item.stack * 10000L; break;
                        case ItemID.PlatinumCoin: val += (long)item.stack * 1000000L; break;
                    }
                }
            }
            return val;
        }
    }

    /// <summary>
    /// 兼容别名类
    /// </summary>
    public static class PortableContainer
    {
        public static GetSetReset<bool> EnableContainerCraft => PortableContainerHooks.EnableContainerCraft;
        public static GetSetReset<bool> EnableMiddleClickOpen => PortableContainerHooks.EnableMiddleClickOpen;
        public static GetSetReset<bool> EnableAutoCoinsToPiggyBank => PortableContainerHooks.EnableAutoCoinsToPiggyBank;
        public static GetSetReset<bool> RequirePiggyBankItem => PortableContainerHooks.RequirePiggyBankItem;

        public static List<CommandObject> GetCO() => PortableContainerHooks.GetCO();
        public static List<UIElement> GetUI() => PortableContainerHooks.GetUI();

        public static void RegisterAll() => PortableContainerHooks.RegisterAll();
        public static void UnregisterAll() => PortableContainerHooks.UnregisterAll();
    }
}
