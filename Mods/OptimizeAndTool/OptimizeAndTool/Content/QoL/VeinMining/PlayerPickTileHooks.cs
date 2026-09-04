using Terraria;

namespace OptimizeAndTool.Content.QoL.VeinMining
{
    /// <summary>
    /// 拦截玩家挖掘图格事件门控，在矿石/宝石被摧毁的瞬间启动连锁挖掘（基于 HookGen 强类型 On_ 门控）
    /// 作者: SaintCirno9
    /// </summary>
    public class PlayerPickTileHooks
    {
        private static bool _registered = false;

        public static void RegisterAll()
        {
            if (_registered) return;
            On_Player.PickTile += Hook_PickTile;
            _registered = true;
        }

        public static void UnregisterAll()
        {
            if (!_registered) return;
            On_Player.PickTile -= Hook_PickTile;
            _registered = false;
        }

        private static void Hook_PickTile(On_Player.orig_PickTile orig, Player self, int x, int y, int pickPower, int dealDamageAsIfBaseNumberIs)
        {
            bool valid = false;
            ushort beforeType = 0;
            short beforeFrameX = 0;

            if (!VeinMiningLogic.IsExecuting && VeinMiningLogic.Enable.val && WorldGen.InWorld(x, y, 1))
            {
                Tile tile = Main.tile[x, y];
                if (tile != null && tile.active())
                {
                    valid = true;
                    beforeType = tile.type;
                    beforeFrameX = tile.frameX;
                }
            }

            orig(self, x, y, pickPower, dealDamageAsIfBaseNumberIs);

            if (!valid || VeinMiningLogic.IsExecuting || !VeinMiningLogic.Enable.val) return;
            if (!WorldGen.InWorld(x, y, 1)) return;

            Tile afterTile = Main.tile[x, y];
            // 若原物块在本次挖掘后已被破坏或类型转变，说明挖掘完成，触发 BFS 连锁
            if (afterTile == null || !afterTile.active() || afterTile.type != beforeType)
            {
                VeinMiningLogic.StartMining(self, x, y, beforeType, beforeFrameX, pickPower);
            }
        }
    }
}
