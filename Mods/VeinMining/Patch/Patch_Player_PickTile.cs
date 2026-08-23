using HarmonyLib;
using Terraria;
using VeinMining.Config;
using VeinMining.Core;

namespace VeinMining.Patch
{
    /// <summary>
    /// Hook 玩家使用镐子/钻头挖掘方块的核心逻辑 (Player.PickTile)
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.PickTile))]
    public static class Patch_Player_PickTile
    {
        public struct MiningState
        {
            public bool IsActive;
            public ushort Type;
            public short FrameX;
            public int X;
            public int Y;
        }

        [HarmonyPrefix]
        public static void Prefix(Player __instance, int x, int y, int pickPower, out MiningState? __state)
        {
            if (VeinMiningLogic.IsExecuting || !VeinMiningConfig.Enable || __instance == null || __instance.whoAmI != Main.myPlayer)
            {
                __state = null;
                return;
            }

            if (x < 0 || x >= Main.maxTilesX || y < 0 || y >= Main.maxTilesY)
            {
                __state = null;
                return;
            }

            Tile tile = Main.tile[x, y];
            if (tile != null && tile.active() && VeinMiningSets.IsMinable(tile.type))
            {
                __state = new MiningState
                {
                    IsActive = true,
                    Type = tile.type,
                    FrameX = tile.frameX,
                    X = x,
                    Y = y
                };
            }
            else
            {
                __state = null;
            }
        }

        [HarmonyPostfix]
        public static void Postfix(Player __instance, int x, int y, int pickPower, MiningState? __state)
        {
            if (VeinMiningLogic.IsExecuting || !VeinMiningConfig.Enable || __instance == null || __instance.whoAmI != Main.myPlayer || __state == null)
            {
                return;
            }

            MiningState state = __state.Value;
            if (!state.IsActive) return;

            // 检查方块是否在此次镐击后被完全破坏
            Tile tile = Main.tile[x, y];
            bool broken = (tile == null || !tile.active() || tile.type != state.Type);

            if (broken)
            {
                VeinMiningLogic.StartMining(__instance, state.X, state.Y, state.Type, state.FrameX, pickPower);
            }
        }
    }
}
