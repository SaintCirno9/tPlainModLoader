using OptimizeAndTool.Content.Storage.Core;
using Terraria.UI;

namespace OptimizeAndTool.Content.Storage.AccessoryBox
{
    /// <summary>
    /// 随身饰品袋 UI 窗口：
    /// 继承自 UniversalBagWindow，提供单例与静态辅助入口，无缝兼容现有代码。
    /// 作者: SaintCirno9
    /// </summary>
    public class AccessoryBagWindow : UniversalBagWindow
    {
        private static AccessoryBagWindow instance = null;
        public static AccessoryBagWindow Instance => instance ?? (instance = new AccessoryBagWindow());

        public static new bool IsOpen => instance != null && instance.Parent != null;
        public static bool IsOpenAndHovering => IsOpen && instance.IsMouseHovering;

        public new AccessoryBagItem CurrentBag => base.CurrentBag as AccessoryBagItem;

        public AccessoryBagWindow() : base("随身饰品袋")
        {
            instance = this;
        }

        public static void Toggle(AccessoryBagItem bag = null)
        {
            if (bag == null)
            {
                bag = CarriedBagCacheManager.GetFirstCarriedBag<AccessoryBagItem>();
            }

            if (IsOpen && Instance.CurrentBag == bag && bag != null)
            {
                Instance.Close();
            }
            else if (bag != null)
            {
                if (ModifyInterfaceLayers.ui_state != null)
                {
                    Instance.Open(bag, ModifyInterfaceLayers.ui_state);
                }
            }
            else if (IsOpen)
            {
                Instance.Close();
            }
        }

        public void Open(AccessoryBagItem bag, UIState parentState)
        {
            if (bag != null)
            {
                bag.ResizeToConfig();
            }
            base.Open(bag, parentState);
        }

        public void DepositAll() => CurrentBag?.DepositAll(Terraria.Main.LocalPlayer);
        public void QuickStack() => CurrentBag?.QuickStack(Terraria.Main.LocalPlayer);
        public void LootAll() => CurrentBag?.LootAll(Terraria.Main.LocalPlayer);
        public void SortBag() => CurrentBag?.Sort();
    }
}
