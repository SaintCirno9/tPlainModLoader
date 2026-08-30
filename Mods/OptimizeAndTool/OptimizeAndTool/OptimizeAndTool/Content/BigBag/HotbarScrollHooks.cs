using OptimizeAndTool.Content.Creative;
using Terraria;
using Terraria.GameInput;

namespace OptimizeAndTool.Content.BigBag
{
    /// <summary>
    /// 当玩家光标在自定义悬浮窗口（大背包/饰品箱/物品浏览器）上时，阻止滚轮误切快捷栏与原版制作滚动（基于 HookGen 强类型 On_ 门控）
    /// 作者: SaintCirno9
    /// </summary>
    public static class HotbarScrollHooks
    {
        private static bool _registered = false;

        public static void RegisterAll()
        {
            if (_registered) return;
            On_Player.HandleHotbarControls += Hook_HandleHotbarControls;
            On_Main.DoScrollingInInventory += Hook_DoScrollingInInventory;
            _registered = true;
        }

        public static void UnregisterAll()
        {
            if (!_registered) return;
            On_Player.HandleHotbarControls -= Hook_HandleHotbarControls;
            On_Main.DoScrollingInInventory -= Hook_DoScrollingInInventory;
            _registered = false;
        }

        private static void Hook_HandleHotbarControls(On_Player.orig_HandleHotbarControls orig, Player self)
        {
            // 仅当玩家确实打开了对应模组窗口且光标悬停在窗口内部时，才拦截快捷栏滚轮
            bool inBigBag = Main.playerInventory && ModifyInterfaceLayers.BigBagIsOpen && ModifyInterfaceLayers.BigBagIsHovering;
            bool inBox = Main.playerInventory && ModifyInterfaceLayers.BoxIsOpen && ModifyInterfaceLayers.BoxIsHovering;
            bool inCreative = CreativeInventory.IsOpen && CreativeInventory.IsHovering;
            bool inPotionBag = Main.playerInventory && ModifyInterfaceLayers.PotionBagIsOpen && ModifyInterfaceLayers.PotionBagIsHovering;
            bool inBannerChest = Main.playerInventory && ModifyInterfaceLayers.BannerChestIsOpen && ModifyInterfaceLayers.BannerChestIsHovering;

            if (inBigBag || inBox || inCreative || inPotionBag || inBannerChest)
            {
                PlayerInput.ScrollWheelDelta = 0;
            }

            orig(self);
        }

        private static void Hook_DoScrollingInInventory(On_Main.orig_DoScrollingInInventory orig)
        {
            // 仅当玩家光标悬停在大背包/饰品箱/物品浏览器/药水袋/旗帜盒内部时，跳过原版制造列表与箱子列表滚动
            bool inBigBag = ModifyInterfaceLayers.BigBagIsOpen && ModifyInterfaceLayers.BigBagIsHovering;
            bool inBox = ModifyInterfaceLayers.BoxIsOpen && ModifyInterfaceLayers.BoxIsHovering;
            bool inCreative = CreativeInventory.IsOpen && CreativeInventory.IsHovering;
            bool inPotionBag = ModifyInterfaceLayers.PotionBagIsOpen && ModifyInterfaceLayers.PotionBagIsHovering;
            bool inBannerChest = ModifyInterfaceLayers.BannerChestIsOpen && ModifyInterfaceLayers.BannerChestIsHovering;

            if (inBigBag || inBox || inCreative || inPotionBag || inBannerChest)
            {
                return;
            }

            orig();
        }
    }
}
