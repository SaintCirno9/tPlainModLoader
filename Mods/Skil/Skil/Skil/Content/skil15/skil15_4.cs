using Microsoft.Xna.Framework;
using Skil.Utils;
using Skil.Utils.quickBuild;
using System.Collections.Generic;
using tContentPatch;
using Terraria;
using Terraria.GameContent.Drawing;
using Terraria.ID;
using Terraria.UI;

namespace Skil.Content.skil15
{
    //和圣骑士锤无关的下落
    internal class skil15_4 : PatchPlayer
    {
        private class enterWorld : PatchMain
        {
            public override void OnEnterWorld()
            {
                runing = false;
            }
        }

        public static GetSetReset<bool> Enable = new GetSetReset<bool>();
        private static bool runing = false;
        private static Vector2 pos = Vector2.Zero;
        private static int count = 0;

        public static List<UIElement> GetUI()
        {
            return new List<UIElement>()
            {
                UIBuild.get2(Enable, "按住下和右键", "Images/Item_1513", "向下猛冲"),
            };
        }

        public override void UpdatePostfix(Player This, int playerI)
        {
            if (This != Main.LocalPlayer) return;
            if (Enable.val == false)
            {
                runing = false;
                return;
            }

            if (runing == false)
            {
                if (This.mouseInterface == true) return;
                if (This.controlDown == false) return;
                if (Main.mouseRight == false) return;
                count = 0;
                pos = This.Center;
                runing = true;
            }

            //if (Main.GameUpdateCount % 2 != 0) return;

            if (AddY(This, ref pos))
            {
                runing = false;

                This.velocity = Vector2.UnitY * -8;
                NetMessage.SendData(MessageID.PlayerControls, number: This.whoAmI);

                SpawLig(pos);
                SpawCattiv(pos);
                SpawP(pos);
                return;
            }

            This.velocity = Vector2.UnitY * 8;
            NetMessage.SendData(MessageID.PlayerControls, number: This.whoAmI);

            ParticleOrchestraType type = ParticleOrchestraType.BestReforge;
            ParticleOrchestrator.BroadcastOrRequestParticleSpawn(type, new ParticleOrchestraSettings
            {
                PositionInWorld = pos,
            });
        }

        private static bool AddY(Player player, ref Vector2 pos)
        {
            ++count;

            for (int i = 0; i < 2; ++i)
            {
                pos.Y += 15;
                Point p = Terraria.Utils.ToTileCoordinates(pos);
                player.fallStart = player.fallStart2 = p.Y;//重置下落高度
                player.Center = pos;

                if (WorldGen.InWorld(p.X, p.Y) == false ||
                    Main.tile[p.X, p.Y]?.active() == true ||
                    count > 120)
                {
                    return true;
                }
            }

            return false;
        }

        private static void SpawP(Vector2 pos)
        {
            int margin = 16 * 3;
            int count = 4;
            float size = count * 0.8f;

            for (int i = 0; i < count; ++i)
            {
                int len = 50 + (margin * i);

                Vector2 p1 = pos;
                Vector2 p2 = pos;
                p1.X += len;
                p2.X -= len;

                int time = -4 * i;

                Projectile.NewProjectile(null, p1, -Vector2.UnitY.RotatedBy(Utils.getRand(0, 10) * 0.1f),
                    ProjectileID.DeadCellsFlintShot, SkilListControl1.damage.val, 1, Main.myPlayer,
                    time, size);
                Projectile.NewProjectile(null, p2, -Vector2.UnitY.RotatedBy(Utils.getRand(0, 10) * -0.1f),
                    ProjectileID.DeadCellsFlintShot, SkilListControl1.damage.val, 1, Main.myPlayer,
                    time, size);

                size -= 0.7f;
            }
        }

        private static void SpawLig(Vector2 pos)//雷
        {
            ParticleOrchestraType type = ParticleOrchestraType.StormLightning;
            ParticleOrchestrator.BroadcastOrRequestParticleSpawn(type, new ParticleOrchestraSettings
            {
                PositionInWorld = pos,
                UniqueInfoPiece = skil15_1.GetLightningColor(),
                MovementVector = new Vector2(Utils.getRand(0, 1145), 0f),
            });
        }

        private static void SpawCattiv(Vector2 pos)//火花
        {
            for (int i = 0; i < 10; ++i)
            {
                float r = Utils.getRand(0, (int)MathHelper.TwoPi);
                r += Utils.getRandFloat();
                int len = Utils.getRand(16 * 3, 16 * 6);

                Vector2 p = Vector2.UnitX.RotatedBy(r) * len;
                p += pos;

                ParticleOrchestraType type = ParticleOrchestraType.CattivaHit;
                ParticleOrchestrator.BroadcastOrRequestParticleSpawn(type, new ParticleOrchestraSettings
                {
                    PositionInWorld = p,
                });
            }
        }
    }
}
