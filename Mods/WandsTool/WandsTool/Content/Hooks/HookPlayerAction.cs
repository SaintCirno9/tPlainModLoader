using Terraria;
using TPML.Core.Logging;

namespace WandsTool.Content.Hooks
{
    /// <summary>
    /// 拦截魔棒模式下的原版手持物动作与世界交互（基于 HookGen 强类型 On_ 门控，零反射，100% 对齐规范）：<br/>
    /// 防止手持物品动作和魔棒选区动作同时触发。<br/>
    /// 作者: SaintCirno9
    /// </summary>
    internal static class HookPlayerAction
    {
        private static readonly ILogger Logger = LogManager.GetLogger("WandsTool");
        private static bool _registered = false;

        public static void RegisterAll()
        {
            if (_registered) return;

            On_Player.ItemCheck += Hook_ItemCheck;
            On_Player.TileInteractionsCheck += Hook_TileInteractionsCheck;
            On_Player.TileInteractionsCheckLongDistance += Hook_TileInteractionsCheckLongDistance;
            On_Player.DropSelectedItem += Hook_DropSelectedItem;

            _registered = true;
            Logger.Info("★ WandsTool MonoMod On_ 门控已成功注册");
        }

        public static void UnregisterAll()
        {
            if (!_registered) return;

            On_Player.ItemCheck -= Hook_ItemCheck;
            On_Player.TileInteractionsCheck -= Hook_TileInteractionsCheck;
            On_Player.TileInteractionsCheckLongDistance -= Hook_TileInteractionsCheckLongDistance;
            On_Player.DropSelectedItem -= Hook_DropSelectedItem;

            _registered = false;
        }

        /// <summary>
        /// 拦截原版 ItemCheck（物块放置、工具挖掘、武器挥舞/射击、药水使用等）
        /// </summary>
        private static void Hook_ItemCheck(On_Player.orig_ItemCheck orig, Player self)
        {
            if (self != null && self.whoAmI == Main.myPlayer && GameMain.Wand_isEnable)
            {
                // 将动作状态强制归零，防止手臂僵直、误挥或引导型武器（如终极棱镜）持续消耗
                self.itemAnimation = 0;
                self.itemTime = 0;
                self.reuseDelay = 0;
                self.channel = false;
                return; // 阻断原版 ItemCheck 执行
            }

            orig(self);
        }

        /// <summary>
        /// 拦截近距离右键世界交互（开箱、开门、开关、标牌等），使右键仅响应魔棒的取消选区与设置轮盘
        /// </summary>
        private static void Hook_TileInteractionsCheck(On_Player.orig_TileInteractionsCheck orig, Player self, int myX, int myY)
        {
            if (self != null && self.whoAmI == Main.myPlayer && GameMain.Wand_isEnable)
            {
                return;
            }

            orig(self, myX, myY);
        }

        /// <summary>
        /// 拦截远距离智能右键交互
        /// </summary>
        private static void Hook_TileInteractionsCheckLongDistance(On_Player.orig_TileInteractionsCheckLongDistance orig, Player self, int myX, int myY)
        {
            if (self != null && self.whoAmI == Main.myPlayer && GameMain.Wand_isEnable)
            {
                return;
            }

            orig(self, myX, myY);
        }

        /// <summary>
        /// 拦截原版丢弃物品动作：魔杖模式下防止光标抓取材料框选时误把整组物品扔在地上。
        /// 背包开启且鼠标悬停 UI 控件上时放行，保证槽位整理/丢弃操作不受影响。
        /// </summary>
        private static void Hook_DropSelectedItem(On_Player.orig_DropSelectedItem orig, Player self)
        {
            if (self != null && self.whoAmI == Main.myPlayer && GameMain.Wand_isEnable)
            {
                // 背包开启且鼠标落在物品槽等 UI 控件上时，允许原版正常操作（拿取/拆分/丢弃）
                if (Main.playerInventory && (Main.LocalPlayer.mouseInterface || Main.editChest))
                {
                    orig(self);
                    return;
                }

                // 其余世界场景（选区进行中、光标抓物悬空等）一律拦截，杜绝误扔
                return;
            }

            orig(self);
        }
    }
}
