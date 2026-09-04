using System;
using OptimizeAndTool.Content.Storage.Core;
using Terraria;
using Terraria.UI;
using TPML.Content;

namespace OptimizeAndTool.Content.Storage.ItemContainer
{
    /// <summary>
    /// 通用容器快捷窗口操作助手
    /// </summary>
    public static class ContainerWindowHelper<T> where T : ItemContainerItem
    {
        public static ItemContainerWindow Instance => ItemContainerWindow.Instance;
        public static bool IsOpen => ItemContainerWindow.IsOpen && ItemContainerWindow.Instance.Container is T;

        public static void Toggle(IItemContainer container = null)
        {
            if (container == null)
            {
                container = FindCarriedContainer();
            }

            if (container != null)
            {
                ItemContainerWindow.Toggle(container);
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

        public static IItemContainer FindCarriedContainer()
        {
            return CarriedBagCacheManager.GetFirstCarriedBag<T>();
        }
    }

    /// <summary>
    /// 药水袋 UI 窗口快捷操作封装
    /// </summary>
    public static class PotionBagWindow
    {
        public static ItemContainerWindow Instance => ContainerWindowHelper<PotionBagItem>.Instance;
        public static bool IsOpen => ContainerWindowHelper<PotionBagItem>.IsOpen;
        public static void Toggle(IItemContainer bag = null) => ContainerWindowHelper<PotionBagItem>.Toggle(bag);
        public static void Close() => ContainerWindowHelper<PotionBagItem>.Close();
        public static IItemContainer FindCarriedContainer() => ContainerWindowHelper<PotionBagItem>.FindCarriedContainer();
    }

    /// <summary>
    /// 旗帜盒 UI 窗口快捷操作封装
    /// </summary>
    public static class BannerChestWindow
    {
        public static ItemContainerWindow Instance => ContainerWindowHelper<BannerChestItem>.Instance;
        public static bool IsOpen => ContainerWindowHelper<BannerChestItem>.IsOpen;
        public static void Toggle(IItemContainer chest = null) => ContainerWindowHelper<BannerChestItem>.Toggle(chest);
        public static void Close() => ContainerWindowHelper<BannerChestItem>.Close();
        public static IItemContainer FindCarriedContainer() => ContainerWindowHelper<BannerChestItem>.FindCarriedContainer();
    }

    /// <summary>
    /// 随身垃圾桶 UI 窗口快捷操作封装
    /// </summary>
    public static class TrashBagWindow
    {
        public static ItemContainerWindow Instance => ContainerWindowHelper<TrashBagItem>.Instance;
        public static bool IsOpen => ContainerWindowHelper<TrashBagItem>.IsOpen;
        public static void Toggle(IItemContainer bag = null) => ContainerWindowHelper<TrashBagItem>.Toggle(bag);
        public static void Close() => ContainerWindowHelper<TrashBagItem>.Close();
        public static IItemContainer FindCarriedContainer() => ContainerWindowHelper<TrashBagItem>.FindCarriedContainer();
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
