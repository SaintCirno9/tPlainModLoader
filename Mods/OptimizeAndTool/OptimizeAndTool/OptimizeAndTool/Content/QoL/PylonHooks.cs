using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;

namespace OptimizeAndTool.Content.QoL
{
    /// <summary>
    /// 全图晶塔无限制传送与放置上限解除门控（基于 HookGen 强类型 On_ 门控）
    /// 作者: SaintCirno9
    /// </summary>
    internal static class PylonHooks
    {
        private static bool _registered = false;

        public static void RegisterAll()
        {
            if (_registered) return;
            On_TeleportPylonsSystem.HasPylonOfType += Hook_HasPylonOfType;
            On_TeleportPylonsSystem.IsPlayerNearAPylon += Hook_IsPlayerNearAPylon;
            On_TeleportPylonsSystem.DoesPylonHaveEnoughNPCsAroundIt += Hook_DoesPylonHaveEnoughNPCsAroundIt;
            On_TeleportPylonsSystem.DoesPositionHaveEnoughNPCs += Hook_DoesPositionHaveEnoughNPCs;
            On_TeleportPylonsSystem.DoesPylonAcceptTeleportation += Hook_DoesPylonAcceptTeleportation;
            On_TeleportPylonsSystem.HandleTeleportRequest += Hook_HandleTeleportRequest;
            _registered = true;
        }

        public static void UnregisterAll()
        {
            if (!_registered) return;
            On_TeleportPylonsSystem.HasPylonOfType -= Hook_HasPylonOfType;
            On_TeleportPylonsSystem.IsPlayerNearAPylon -= Hook_IsPlayerNearAPylon;
            On_TeleportPylonsSystem.DoesPylonHaveEnoughNPCsAroundIt -= Hook_DoesPylonHaveEnoughNPCsAroundIt;
            On_TeleportPylonsSystem.DoesPositionHaveEnoughNPCs -= Hook_DoesPositionHaveEnoughNPCs;
            On_TeleportPylonsSystem.DoesPylonAcceptTeleportation -= Hook_DoesPylonAcceptTeleportation;
            On_TeleportPylonsSystem.HandleTeleportRequest -= Hook_HandleTeleportRequest;
            _registered = false;
        }

        private static bool Hook_HasPylonOfType(On_TeleportPylonsSystem.orig_HasPylonOfType orig, TeleportPylonsSystem self, TeleportPylonType pylonType)
        {
            if (QoLValSet.pylonUnlimitedPlacement.val)
            {
                return false;
            }
            return orig(self, pylonType);
        }

        private static bool Hook_IsPlayerNearAPylon(On_TeleportPylonsSystem.orig_IsPlayerNearAPylon orig, Player player)
        {
            if (QoLValSet.pylonFreeTeleport.val)
            {
                return true;
            }
            return orig(player);
        }

        private static bool Hook_DoesPylonHaveEnoughNPCsAroundIt(On_TeleportPylonsSystem.orig_DoesPylonHaveEnoughNPCsAroundIt orig, TeleportPylonsSystem self, TeleportPylonInfo info, int numberOfNPCsRequiredToGenerateTeleportationMarker)
        {
            if (QoLValSet.pylonFreeTeleport.val)
            {
                return true;
            }
            return orig(self, info, numberOfNPCsRequiredToGenerateTeleportationMarker);
        }

        private static bool Hook_DoesPositionHaveEnoughNPCs(On_TeleportPylonsSystem.orig_DoesPositionHaveEnoughNPCs orig, int numberOfNPCsRequiredToGenerateTeleportationMarker, Point16 centerTileCoords)
        {
            if (QoLValSet.pylonFreeTeleport.val)
            {
                return true;
            }
            return orig(numberOfNPCsRequiredToGenerateTeleportationMarker, centerTileCoords);
        }

        private static bool Hook_DoesPylonAcceptTeleportation(On_TeleportPylonsSystem.orig_DoesPylonAcceptTeleportation orig, TeleportPylonsSystem self, TeleportPylonInfo info, Player player)
        {
            if (QoLValSet.pylonFreeTeleport.val)
            {
                return true;
            }
            return orig(self, info, player);
        }

        private static void Hook_HandleTeleportRequest(On_TeleportPylonsSystem.orig_HandleTeleportRequest orig, TeleportPylonsSystem self, TeleportPylonInfo info, int playerIndex)
        {
            if (!QoLValSet.pylonFreeTeleport.val)
            {
                orig(self, info, playerIndex);
                return;
            }

            if (playerIndex < 0 || playerIndex >= Main.player.Length) return;
            Player player = Main.player[playerIndex];
            if (player == null || !player.active || player.dead) return;

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
        }
    }

    /// <summary>
    /// 兼容别名类
    /// </summary>
    internal static class Patch_Pylon
    {
    }
}
