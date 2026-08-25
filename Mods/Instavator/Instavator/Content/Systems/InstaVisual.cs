using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using TPML.Content;
using Terraria.UI;

namespace Instavator.Content.Systems
{
    /// <summary>
    /// 直通车选区半透明高亮预览渲染系统
    /// </summary>
    public class InstaVisualSystem : ModSystem
    {
        private static Vector2? _drawPos;
        private static int _blockWidth;
        private static int _blockHeight;
        private static Color _boxColor;
        private static Texture2D _pixelTexture;

        public static void RequestVisual(Vector2 mouseWorld, int blockWidth, int blockHeight, Color color)
        {
            _drawPos = mouseWorld;
            _blockWidth = blockWidth;
            _blockHeight = blockHeight;
            _boxColor = color;
        }

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
            if (!_drawPos.HasValue) return true;

            try
            {
                SpriteBatch sb = Main.spriteBatch;
                if (sb == null) return true;

                if (_pixelTexture == null || _pixelTexture.IsDisposed)
                {
                    _pixelTexture = new Texture2D(sb.GraphicsDevice, 1, 1);
                    _pixelTexture.SetData(new[] { Color.White });
                }

                Vector2 center = _drawPos.Value;
                int tileHalfW = _blockWidth / 2;
                int startTileX = (int)(center.X / 16f) - tileHalfW;
                int startTileY = (int)(center.Y / 16f);

                Vector2 screenPos = new Vector2(startTileX * 16f, startTileY * 16f) - Main.screenPosition;
                int pixelW = _blockWidth * 16;
                int pixelH = Math.Min(_blockHeight, Main.maxTilesY - startTileY - 40) * 16;

                if (pixelH > 0 && pixelW > 0)
                {
                    Rectangle rect = new Rectangle((int)screenPos.X, (int)screenPos.Y, pixelW, pixelH);
                    sb.Draw(_pixelTexture, rect, _boxColor * 0.35f);

                    // 边框高亮
                    sb.Draw(_pixelTexture, new Rectangle(rect.X, rect.Y, rect.Width, 2), _boxColor * 0.8f);
                    sb.Draw(_pixelTexture, new Rectangle(rect.X, rect.Y, 2, rect.Height), _boxColor * 0.8f);
                    sb.Draw(_pixelTexture, new Rectangle(rect.Right - 2, rect.Y, 2, rect.Height), _boxColor * 0.8f);
                    sb.Draw(_pixelTexture, new Rectangle(rect.X, rect.Bottom - 2, rect.Width, 2), _boxColor * 0.8f);
                }
            }
            catch
            {
            }
            finally
            {
                _drawPos = null;
            }

            return true;
        }
    }
}
