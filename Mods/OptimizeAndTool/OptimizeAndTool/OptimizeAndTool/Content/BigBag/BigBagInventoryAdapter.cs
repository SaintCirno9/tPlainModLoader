using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using OptimizeAndTool.Content.Storage.Core;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace OptimizeAndTool.Content.BigBag
{
    /// <summary>
    /// 巨大背包通用 IBagInventory 适配器：
    /// 将全局/角色级 BigBag.Slots 接入通用容器数据契约与 UniversalBagWindow 框架
    /// 作者: SaintCirno9
    /// </summary>
    public class BigBagInventoryAdapter : IBagInventory, IToolbarCustomActions
    {
        public string Title => "巨大背包";
        public Item[] Slots => BigBag.Slots;
        public int Capacity => BigBag.Slots != null ? BigBag.Slots.Length : BigBag.Capacity.val;
        public bool CanFavorite => true;
        public bool ShowModSidebar => true;
        public bool ShowFilterBar => true;

        public event Action OnSlotsChanged;
        public void TriggerSlotsChanged() => OnSlotsChanged?.Invoke();

        public BigBagInventoryAdapter()
        {
            BigBag.OnCapacityChanged += TriggerSlotsChanged;
        }

        public bool MeetEntryCriteria(Item item, int targetSlot = -1)
        {
            return item != null && !item.IsAir;
        }

        public bool TryDeposit(Item item, bool sort = true)
        {
            return BigBag.DepositItem(item, -1);
        }

        public bool TryDepositFromSlot(Item[] inv, int slot, bool justCheck)
        {
            return BigBag.TryPlacingInBigBag(inv, slot, justCheck);
        }

        public void DepositAll(Player player)
        {
            BigBag.DepositAllFromPlayer(player);
        }

        public void QuickStack(Player player)
        {
            BigBag.QuickStackFromPlayer(player);
        }

        public void LootAll(Player player, Func<Item, bool> filter = null)
        {
            BigBag.LootAllToPlayer(player, filter);
        }

        public void Sort()
        {
            BigBag.SortBigBag();
        }

        public string GetCapacityText()
        {
            int filled = 0;
            Item[] slots = BigBag.Slots;
            if (slots != null)
            {
                for (int i = 0; i < slots.Length; i++)
                {
                    if (slots[i] != null && !slots[i].IsAir) filled++;
                }
                return $"已存: {filled}/{slots.Length} (动态)";
            }
            return "0/0";
        }

        public bool IsDynamicCapacity => true;
        public void EnsureTrailingEmptySlots(int trailingCount = 10) => BigBag.EnsureTrailingEmptySlots(trailingCount);
        public void ExpandCapacity(int addedCount) => BigBag.ExpandCapacity(addedCount);

        public IEnumerable<BagToolbarButton> GetCustomToolbarButtons()
        {
            yield return new BagToolbarButton(
                () => $"拾取时自动堆入大背包: {(BigBag.AutoStackOnPickup.val ? "[开启]" : "[关闭]")}",
                () => "Images/Item_5010",
                () =>
                {
                    BigBag.AutoStackOnPickup.val = !BigBag.AutoStackOnPickup.val;
                    SoundEngine.PlaySound(SoundID.MenuTick);
                    TriggerSlotsChanged();
                },
                () => BigBag.AutoStackOnPickup.val ? Color.White : Color.Gray * 0.5f
            );
        }
    }
}
