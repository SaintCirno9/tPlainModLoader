using HarmonyLib;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent;
using Terraria.GameInput;

namespace OptimizeAndTool.Content.QoL.InfiniteBuff
{
    /// <summary>
    /// 原版 Buff 图标交互补丁：
    /// 鼠标悬停左上角 Buff 图标时追加操作提示，左键点击直接快捷呼出/关闭无限增益管理窗口
    /// 作者: SaintCirno9
    /// </summary>
    [HarmonyPatch]
    public static class Patch_BuffInteraction
    {
        [HarmonyPatch(typeof(Main), nameof(Main.DrawBuffIcon))]
        [HarmonyPostfix]
        public static void DrawBuffIconPostfix(int drawBuffText, int buffSlotOnPlayer, int x, int y, ref int __result)
        {
            if (Main.netMode == 2 || Main.gameMenu) return;

            Player player = Main.LocalPlayer;
            if (player == null || buffSlotOnPlayer < 0 || buffSlotOnPlayer >= player.buffType.Length) return;

            int buffType = player.buffType[buffSlotOnPlayer];
            if (buffType <= 0 || buffType >= TextureAssets.Buff.Length) return;

            if (TextureAssets.Buff[buffType]?.Value == null) return;

            int iconWidth = TextureAssets.Buff[buffType].Width();
            int iconHeight = TextureAssets.Buff[buffType].Height();

            if (Main.mouseX >= x && Main.mouseX <= x + iconWidth &&
                Main.mouseY >= y && Main.mouseY <= y + iconHeight &&
                !PlayerInput.IgnoreMouseInterface)
            {
                player.mouseInterface = true;

                if (Main.mouseLeft && Main.mouseLeftRelease)
                {
                    Main.mouseLeftRelease = false;
                    InfiniteBuffWindow.Instance.Toggle();
                }
            }
        }

        [HarmonyPatch(typeof(Main), nameof(Main.GetBuffTooltip))]
        [HarmonyPostfix]
        public static void GetBuffTooltipPostfix(Player player, int buffType, ref string __result)
        {
            if (buffType > 0 && !string.IsNullOrEmpty(__result))
            {
                __result += "\n[c/88FF88:[左键]] 打开/关闭增益管理窗口";
            }
        }
    }
}
