using HarmonyLib;
using Terraria;

namespace OptimizeAndTool.Content.QoL.VeinMining
{
    /// <summary>
    /// 拦截玩家挖掘图格事件，在矿石/宝石被摧毁的瞬间启动连锁挖掘
    /// 作者: SaintCirno9
    /// </summary>
    [HarmonyPatch(typeof(Player), "PickTile")]
    public class Patch_Player_PickTile
    {
        [HarmonyPrefix]
        public static void Prefix(Player __instance, int x, int y, int pickPower, out (bool valid, ushort type, short frameX) __state)
        {
            __state = (false, 0, 0);

            if (VeinMiningLogic.IsExecuting || !VeinMiningLogic.Enable.val) return;
            if (!WorldGen.InWorld(x, y, 1)) return;

            Tile tile = Main.tile[x, y];
            if (tile != null && tile.active())
            {
                __state = (true, tile.type, tile.frameX);
            }
        }

        [HarmonyPostfix]
        public static void Postfix(Player __instance, int x, int y, int pickPower, (bool valid, ushort type, short frameX) __state)
        {
            if (!__state.valid || VeinMiningLogic.IsExecuting || !VeinMiningLogic.Enable.val) return;
            if (!WorldGen.InWorld(x, y, 1)) return;

            Tile tile = Main.tile[x, y];
            // 若原物块在本次挖掘后已被破坏或类型转变，说明挖掘完成，触发 BFS 连锁
            if (tile == null || !tile.active() || tile.type != __state.type)
            {
                VeinMiningLogic.StartMining(__instance, x, y, __state.type, __state.frameX, pickPower);
            }
        }
    }
}
