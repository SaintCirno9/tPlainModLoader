using FargoItems.Content.Items.Explosives;

using Microsoft.Xna.Framework;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using TPML.Content;

namespace FargoItems.Content.Projectiles.Explosives
{
    public class MiniDirtInstabridgeProj : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Mini Instabridge");
        }

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 36;
            Projectile.aiStyle = 16;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 1;
        }

        public override bool? CanDamage()
        {
            return false;
        }

        public override void AI()
        {
            Projectile.Kill();
        }

        public override void OnKill(int timeLeft)
        {
            Vector2 position = Projectile.Center;
            SoundEngine.PlaySound(SoundID.Item14, position);
            Player player = Main.player[Projectile.owner];
            BuildBridge(player, position);
        }

        public static void BuildBridge(Player player, Vector2 position)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            var logger = TPML.Core.Logging.LogManager.GetLogger("MiniDirtBridge");
            const int length = 150;
            bool goLeft = position.X < player.Center.X;
            int min = goLeft ? -length : 0;
            int max = goLeft ? 0 : length;

            int originX = (int)(position.X / 16.0f);
            int originY = (int)(position.Y / 16.0f);

            logger.Info($"[MiniDirtBridge] 构建迷你土桥, 坐标: ({position.X:F1}, {position.Y:F1}) [图格: {originX}, {originY}], 方向: {(goLeft ? "左" : "右")}, 长度: {length}");

            int[] deletableTiles = [
                TileID.Cactus,
                TileID.Trees,
                TileID.CorruptThorns,
                TileID.CrimsonThorns,
                TileID.JungleThorns
            ];

            for (int x = min; x < max; x++)
            {
                int xPosition = originX + x;
                int yPosition = originY;

                if (!WorldGen.InWorld(xPosition, yPosition))
                    continue;

                Tile tile = Main.tile[xPosition, yPosition];
                if (tile == null)
                {
                    tile = new Tile();
                    Main.tile[xPosition, yPosition] = tile;
                }

                if (tile.active() && deletableTiles.Contains(tile.type))
                {
                    FargoItems.Content.Logic.ExplosivesHelper.ClearEverything(xPosition, yPosition, false);
                }

                // 铺设泥土块
                WorldGen.PlaceTile(xPosition, yPosition, TileID.Dirt, mute: true, forced: true);
                WorldGen.SquareTileFrame(xPosition, yPosition, true);
            }
            logger.Info("[MiniDirtBridge] ★ 泥土平台生成完毕！");
        }
    }
}
