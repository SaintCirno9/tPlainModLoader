using System;
using OptimizeAndTool.Content.Storage.Core;
using Terraria;
using Terraria.UI;
using TPML.Content;

namespace OptimizeAndTool.Content.Storage.ItemContainer
{
    /// <summary>
    /// 药水袋 UI 窗口快捷操作封装
    /// </summary>
    public static class PotionBagWindow
    {
        public static ItemContainerWindow Instance => ItemContainerWindow.Instance;
        public static bool IsOpen => ItemContainerWindow.IsOpen && ItemContainerWindow.Instance.Container is PotionBagItem;

        public static void Toggle(IItemContainer bag = null)
        {
            if (bag == null)
            {
                bag = FindCarriedContainer<PotionBagItem>();
            }

            if (bag != null)
            {
                ItemContainerWindow.Toggle(bag);
            }
            else if (ItemContainerWindow.IsOpen)
            {
                ItemContainerWindow.Instance.Close();
            }
        }

        public static void Close()
        {
            if (IsOpen) ItemContainerWindow.Instance.Close();
        }

        private static IItemContainer FindCarriedContainer<T>() where T : ItemContainerItem
        {
            Player player = Main.LocalPlayer;
            if (player == null) return null;

            int targetType = ModContent.ItemType<T>();
            if (targetType <= 0) return null;

            if (player.inventory != null)
            {
                for (int i = 0; i < player.inventory.Length; i++)
                {
                    Item it = player.inventory[i];
                    if (it != null && !it.IsAir && it.type == targetType)
                    {
                        return ItemLoader.GetModItem(it) as IItemContainer;
                    }
                }
            }

            Item[][] banks = new[] { player.bank?.item, player.bank2?.item, player.bank3?.item, player.bank4?.item };
            foreach (var bank in banks)
            {
                if (bank == null) continue;
                for (int i = 0; i < bank.Length; i++)
                {
                    Item it = bank[i];
                    if (it != null && !it.IsAir && it.type == targetType)
                    {
                        return ItemLoader.GetModItem(it) as IItemContainer;
                    }
                }
            }

            return null;
        }
    }

    /// <summary>
    /// 旗帜盒 UI 窗口快捷操作封装
    /// </summary>
    public static class BannerChestWindow
    {
        public static ItemContainerWindow Instance => ItemContainerWindow.Instance;
        public static bool IsOpen => ItemContainerWindow.IsOpen && ItemContainerWindow.Instance.Container is BannerChestItem;

        public static void Toggle(IItemContainer chest = null)
        {
            if (chest == null)
            {
                chest = FindCarriedContainer<BannerChestItem>();
            }

            if (chest != null)
            {
                ItemContainerWindow.Toggle(chest);
            }
            else if (ItemContainerWindow.IsOpen)
            {
                ItemContainerWindow.Instance.Close();
            }
        }

        public static void Close()
        {
            if (IsOpen) ItemContainerWindow.Instance.Close();
        }

        private static IItemContainer FindCarriedContainer<T>() where T : ItemContainerItem
        {
            Player player = Main.LocalPlayer;
            if (player == null) return null;

            int targetType = ModContent.ItemType<T>();
            if (targetType <= 0) return null;

            if (player.inventory != null)
            {
                for (int i = 0; i < player.inventory.Length; i++)
                {
                    Item it = player.inventory[i];
                    if (it != null && !it.IsAir && it.type == targetType)
                    {
                        return ItemLoader.GetModItem(it) as IItemContainer;
                    }
                }
            }

            Item[][] banks = new[] { player.bank?.item, player.bank2?.item, player.bank3?.item, player.bank4?.item };
            foreach (var bank in banks)
            {
                if (bank == null) continue;
                for (int i = 0; i < bank.Length; i++)
                {
                    Item it = bank[i];
                    if (it != null && !it.IsAir && it.type == targetType)
                    {
                        return ItemLoader.GetModItem(it) as IItemContainer;
                    }
                }
            }

            return null;
        }
    }

    /// <summary>
    /// 通用大型实体收纳容器窗口（继承自 UniversalBagWindow）
    /// 作者: SaintCirno9
    /// </summary>
    public class ItemContainerWindow : UniversalBagWindow
    {
        private static ItemContainerWindow instance;
        public static ItemContainerWindow Instance => instance ?? (instance = new ItemContainerWindow());
        public static new bool IsOpen => instance != null && instance.Parent != null;

        public IItemContainer Container => CurrentBag as IItemContainer;

        public ItemContainerWindow() : base("收纳容器")
        {
            instance = this;
        }

        public static void Toggle(IItemContainer container)
        {
            if (container == null) return;

            if (IsOpen && Instance.Container == container)
            {
                Instance.Close();
            }
            else
            {
                if (ModifyInterfaceLayers.ui_state != null)
                {
                    Instance.Open(container, ModifyInterfaceLayers.ui_state);
                }
            }
        }
    }
}
