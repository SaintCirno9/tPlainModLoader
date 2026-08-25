using System;
using System.Collections.Generic;
using Instavator.Content.Items;
using Instavator.Content.Logic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.UI;
using TPML.Content;

namespace Instavator.Content.Systems
{
    /// <summary>
    /// 直通车选区半透明高亮预览渲染系统
    /// </summary>
    public class InstaVisualSystem : ModSystem
    {
        private static Texture2D _pixelTexture;

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            int layerIndex = layers.FindIndex(l => l.Name.Equals("Vanilla: Cursor", StringComparison.OrdinalIgnoreCase));
            if (layerIndex != -1)
            {
                layers.Insert(layerIndex, new LegacyGameInterfaceLayer("Instavator: Area Preview", DrawAreaPreview, InterfaceScaleType.Game));
            }
        }

        private bool DrawAreaPreview()
        {
            if (Main.gameMenu || Main.dedServ) return true;

            Player player = Main.LocalPlayer;
            if (player == null || !player.active || player.dead || InstavatorShaftBuilder.IsBuildRunning)
            {
                return true;
            }

            Item heldItem = player.HeldItem;
            if (heldItem == null || heldItem.IsAir) return true;

            int blockWidth;
            int targetEndY;
            Color boxColor;

            int type = heldItem.type;
            if (type == ModContent.ItemType<Instavator.Content.Items.Instavator>())
            {
                blockWidth = 7;
                targetEndY = Main.maxTilesY - 40;
                boxColor = new Color(255, 140, 40);
            }
            else if (type == ModContent.ItemType<HalfInstavator>())
            {
                blockWidth = 5;
                targetEndY = (int)(Main.rockLayer + ((Main.maxTilesY - 200) - Main.rockLayer) / 2.0);
                boxColor = new Color(100, 200, 255);
            }
            else if (type == ModContent.ItemType<DoubleObsidianInstavator>())
            {
                blockWidth = 11;
                targetEndY = Main.maxTilesY - 40;
                boxColor = new Color(200, 100, 255);
            }
            else
            {
                return true;
            }

            try
            {
                SpriteBatch sb = Main.spriteBatch;
                if (sb == null) return true;

                if (_pixelTexture == null || _pixelTexture.IsDisposed)
                {
                    _pixelTexture = new Texture2D(sb.GraphicsDevice, 1, 1);
                    _pixelTexture.SetData(new[] { Color.White });
                }

                Vector2 center = Main.MouseWorld;
                int tileHalfW = blockWidth / 2;
                int startTileX = (int)(center.X / 16f) - tileHalfW;
                int startTileY = (int)(center.Y / 16f);

                Vector2 screenPos = new Vector2(startTileX * 16f, startTileY * 16f) - Main.screenPosition;
                int pixelW = blockWidth * 16;
                int depthTiles = Math.Max(0, targetEndY - startTileY);
                int pixelH = depthTiles * 16;

                if (pixelH > 0 && pixelW > 0)
                {
                    Rectangle rect = new Rectangle((int)screenPos.X, (int)screenPos.Y, pixelW, pixelH);
                    sb.Draw(_pixelTexture, rect, boxColor * 0.35f);

                    // 边框高亮
                    sb.Draw(_pixelTexture, new Rectangle(rect.X, rect.Y, rect.Width, 2), boxColor * 0.8f);
                    sb.Draw(_pixelTexture, new Rectangle(rect.X, rect.Y, 2, rect.Height), boxColor * 0.8f);
                    sb.Draw(_pixelTexture, new Rectangle(rect.Right - 2, rect.Y, 2, rect.Height), boxColor * 0.8f);
                    sb.Draw(_pixelTexture, new Rectangle(rect.X, rect.Bottom - 2, rect.Width, 2), boxColor * 0.8f);
                }
            }
            catch
            {
            }

            return true;
        }
    }
}
