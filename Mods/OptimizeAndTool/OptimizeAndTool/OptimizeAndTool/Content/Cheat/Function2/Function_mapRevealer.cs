using CommandHelp;
using Microsoft.Xna.Framework;
using OptimizeAndTool.Utils;
using OptimizeAndTool.Utils.quickBuild;
using System.Collections.Generic;
using System.Threading.Tasks;
using tContentPatch;
using tContentPatch.Threading;
using Terraria;
using Terraria.ID;
using Terraria.UI;

namespace OptimizeAndTool.Content.Cheat.Function2
{
    public class Function_mapRevealer : TPML.Content.ModPlayer
    {
        public class mapRevealer_Unload : Mod
        {
            public override void Unload()
            {
                MapRevealer_runing.val = false;
            }
        }

        public class playState : TPML.Content.ModSystem
        {
            public static bool _Update_noPlay = true;
            public static bool Update_noPlay = true;
            public override void UpdatePrefix(GameTime gameTime)
            {
                _Update_noPlay = true;
            }

            public override void UpdatePostfix(GameTime gameTime)
            {
                Update_noPlay = _Update_noPlay;
            }
        }

        public static List<CommandObject> GetCO()
        {
            List<CommandObject> cos = new List<CommandObject>
            {
                CommandBuild.get2("mapRevealer", MapRevealer_runing),
            };

            return cos;
        }

        public static List<UIElement> GetUI()
        {
            List<UIElement> uis = new List<UIElement>
            {
                UIBuild.get2(MapRevealer_runing, "关闭以取消", text: "点亮全图"),
            };

            return uis;
        }

        public override void UpdatePrefix(Player This, int playerI)
        {
            playState._Update_noPlay = false;

            if (This != Main.LocalPlayer) return;

            a1_MapRevealer(This);
        }

        public static GetSetReset<bool> MapRevealer_runing = new GetSetReset<bool>(false, false);
        private static bool MapRevealer_taskRuning = false;
        private static int _lightSectionX;
        private static int _lightSectionY;
        private static Vector2 _savedPosition;
        private static bool _hasSavedPosition;

        private static void a1_MapRevealer(Player player)
        {
            if (MapRevealer_runing.val == false) return;
            if (MapRevealer_taskRuning) return;

            MapRevealer_runing.val = true;
            MapRevealer_taskRuning = true;
            _lightSectionX = 0;
            _lightSectionY = 0;
            _hasSavedPosition = false;

            if (Main.netMode == 1)
            {
                Main.NewText("加载方块并点亮中");
                _ = Task.Run(() => LoadUnloadedSectionsThenLight(player));
            }
            else
            {
                Main.NewText("点亮中");
            }
        }

        public override void UpdatePostfix(Player This, int playerI)
        {
            if (This != Main.LocalPlayer || !MapRevealer_taskRuning || Main.netMode == 1) return;
            PumpLightMap(This, sectionsPerFrame: 4);
        }

        private static void LoadUnloadedSectionsThenLight(Player player)
        {
            try
            {
                int who = player.whoAmI;
                int width = player.width;
                int height = player.height;

                for (int x = 0; x < Main.maxTilesX; x++)
                {
                    for (int y = 0; y < Main.maxTilesY; y++)
                    {
                        if (Main.tile[x, y] != null) continue;

                        while (Main.tile[x, y] == null)
                        {
                            if (playState.Update_noPlay || MapRevealer_runing.val == false)
                            {
                                MainThreadDispatcher.Enqueue(() => FinishMapReveal(cancelled: true));
                                return;
                            }

                            int cx = x;
                            int cy = y;
                            MainThreadDispatcher.Enqueue(() =>
                            {
                                Player p = Main.player[who];
                                if (p == null) return;
                                if (!_hasSavedPosition)
                                {
                                    _savedPosition = p.position;
                                    _hasSavedPosition = true;
                                }
                                Vector2 v = new Vector2(cx, cy) * 16;
                                v.X = MathHelper.Clamp(v.X, 41 * 16, (Main.maxTilesX - 42) * 16 - width);
                                v.Y = MathHelper.Clamp(v.Y, 41 * 16, (Main.maxTilesY - 42) * 16 - height);
                                p.position = v;
                                NetMessage.SendData(MessageID.PlayerControls, -1, -1, null, who);
                            });

                            Task.Delay(5).GetAwaiter().GetResult();
                        }
                    }
                }

                MainThreadDispatcher.Enqueue(() =>
                {
                    Player p = Main.player[who];
                    RestoreSavedPosition(p);
                    Main.NewText("区块已加载，开始点亮");
                    PumpLightMapUntilDone(p);
                });
            }
            catch (System.Exception ex)
            {
                MainThreadDispatcher.Enqueue(() =>
                {
                    Main.NewText("点亮失败: " + ex.Message);
                    FinishMapReveal(cancelled: true);
                });
            }
        }

        private static void PumpLightMapUntilDone(Player player)
        {
            if (!MapRevealer_taskRuning) return;
            PumpLightMap(player, sectionsPerFrame: 8);
            if (MapRevealer_taskRuning)
            {
                MainThreadDispatcher.Enqueue(() => PumpLightMapUntilDone(player));
            }
        }

        private static void PumpLightMap(Player player, int sectionsPerFrame)
        {
            if (playState.Update_noPlay || MapRevealer_runing.val == false)
            {
                RestoreSavedPosition(player);
                FinishMapReveal(cancelled: true);
                return;
            }

            if (Main.Map == null)
            {
                RestoreSavedPosition(player);
                FinishMapReveal(cancelled: true);
                Main.NewText("更新光照失败");
                return;
            }

            int maxSX = (Main.maxTilesX + 199) / 200;
            int maxSY = (Main.maxTilesY + 149) / 150;
            int n = 0;
            while (n < sectionsPerFrame && _lightSectionX < maxSX)
            {
                Main.Map.UnlockMapSection(_lightSectionX, _lightSectionY);
                n++;
                _lightSectionY++;
                if (_lightSectionY >= maxSY)
                {
                    _lightSectionY = 0;
                    _lightSectionX++;
                }
            }

            if (_lightSectionX >= maxSX)
            {
                RestoreSavedPosition(player);
                Main.refreshMap = true;
                Main.NewText("点亮完成");
                FinishMapReveal(cancelled: false);
            }
        }

        private static void RestoreSavedPosition(Player player)
        {
            if (_hasSavedPosition && player != null)
            {
                player.position = _savedPosition;
                _hasSavedPosition = false;
            }
        }

        private static void FinishMapReveal(bool cancelled)
        {
            MapRevealer_taskRuning = false;
            MapRevealer_runing.val = false;
            if (cancelled) Main.refreshMap = true;
        }
    }
}
