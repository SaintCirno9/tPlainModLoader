using HarmonyLib;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;

namespace SundryTool.Content.QoL
{
    /// <summary>
    /// 全图晶塔无限制传送与放置上限解除
    /// 作者: SaintCirno9
    /// </summary>
    [HarmonyPatch(typeof(TeleportPylonsSystem))]
    internal static class Patch_Pylon
    {
        /// <summary>
        /// 解除同类型晶塔放置数量限制（原版默认单世界每种晶塔只能放1个，万能晶塔2个）
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(TeleportPylonsSystem.HasPylonOfType))]
        public static bool HasPylonOfType_Prefix(ref bool __result)
        {
            if (QoLValSet.pylonUnlimitedPlacement.val)
            {
                __result = false;
                return false;
            }
            return true;
        }

        /// <summary>
        /// 无需靠近晶塔即可传送
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(TeleportPylonsSystem.IsPlayerNearAPylon), new[] { typeof(Player) })]
        public static bool IsPlayerNearAPylon_Prefix(ref bool __result)
        {
            if (QoLValSet.pylonFreeTeleport.val)
            {
                __result = true;
                return false;
            }
            return true;
        }

        /// <summary>
        /// 无需周围有 NPC (检查1)
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch("DoesPylonHaveEnoughNPCsAroundIt")]
        public static bool DoesPylonHaveEnoughNPCsAroundIt_Prefix(ref bool __result)
        {
            if (QoLValSet.pylonFreeTeleport.val)
            {
                __result = true;
                return false;
            }
            return true;
        }

        /// <summary>
        /// 无需周围有 NPC (检查2)
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(TeleportPylonsSystem.DoesPositionHaveEnoughNPCs))]
        public static bool DoesPositionHaveEnoughNPCs_Prefix(ref bool __result)
        {
            if (QoLValSet.pylonFreeTeleport.val)
            {
                __result = true;
                return false;
            }
            return true;
        }

        /// <summary>
        /// 允许接受传送（无视危险、无视生物群落、无视NPC）
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch("DoesPylonAcceptTeleportation")]
        public static bool DoesPylonAcceptTeleportation_Prefix(ref bool __result)
        {
            if (QoLValSet.pylonFreeTeleport.val)
            {
                __result = true;
                return false;
            }
            return true;
        }

        /// <summary>
        /// 晶塔全图无限制直接瞬传
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(TeleportPylonsSystem.HandleTeleportRequest))]
        public static bool HandleTeleportRequest_Prefix(TeleportPylonsSystem __instance, TeleportPylonInfo info, int playerIndex)
        {
            if (!QoLValSet.pylonFreeTeleport.val) return true;

            if (playerIndex < 0 || playerIndex >= Main.player.Length) return true;
            Player player = Main.player[playerIndex];
            if (player == null || !player.active || player.dead) return true;

            Vector2 targetPos = info.PositionInTiles.ToWorldCoordinates() + new Vector2(0f, -player.height) + new Vector2(0f, 6f);
            targetPos.X = MathHelper.Clamp(targetPos.X, 16f, (Main.maxTilesX - 2) * 16f);
            targetPos.Y = MathHelper.Clamp(targetPos.Y, 16f, (Main.maxTilesY - 2) * 16f);

            player.Teleport(targetPos, 4);
            player.velocity = Vector2.Zero;
            player.fallStart = player.fallStart2 = (int)(player.position.Y / 16f);

            if (Main.netMode == 1)
            {
                NetMessage.SendData(65, -1, -1, null, 0, playerIndex, targetPos.X, targetPos.Y, 4);
            }
            return false;
        }
    }
}
