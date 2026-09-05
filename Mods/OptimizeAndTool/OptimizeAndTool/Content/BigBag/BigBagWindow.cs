using OptimizeAndTool.Content.Storage.Core;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.UI;

namespace OptimizeAndTool.Content.BigBag
{
    /// <summary>
    /// 巨大背包窗口：
    /// 继承自 UniversalBagWindow，采用原版官方物品栏材质、Mod 筛选侧栏与紧凑排版。
    /// 作者: SaintCirno9
    /// </summary>
    public class BigBagWindow : UniversalBagWindow
    {
        private static BigBagWindow instance = null;
        public static BigBagWindow Instance => instance ?? (instance = new BigBagWindow());

        public BigBagWindow() : base("巨大背包")
        {
            instance = this;

            OnOpen += () =>
            {
                if (Main.LocalPlayer != null && BigBagStorage.ActivePlayerName != Main.LocalPlayer.name)
                {
                    BigBagStorage.LoadForPlayer(Main.LocalPlayer);
                }

                if (!Main.playerInventory)
                {
                    Main.playerInventory = true;
                    SoundEngine.PlaySound(SoundID.MenuOpen);
                }
            };

            OnClose += () =>
            {
                if (OptimizeAndTool.Content.QoL.Reforge.ReforgeOptimization.PortableReforgeActive)
                {
                    OptimizeAndTool.Content.QoL.Reforge.ReforgeOptimization.TogglePortableReforge();
                }
                if (PrefixWhitelistWindow.Instance.IsOpen)
                {
                    PrefixWhitelistWindow.Instance.Close();
                }
            };
        }

        public void Open(UIState parentState)
        {
            base.Open(BigBag.Inventory, parentState);
        }
    }
}
