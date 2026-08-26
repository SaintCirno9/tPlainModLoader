using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RecipeBrowser.Common;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Map;
using Terraria.ObjectData;

namespace RecipeBrowser
{
    /// <summary>
    /// RecipeBrowser 核心通用工具与图格渲染器
    /// 作者: SaintCirno9
    /// </summary>
    internal static class Utilities
    {
        internal static Dictionary<int, Texture2D> tileTextures = new Dictionary<int, Texture2D>();

        internal static Color textColor = Color.White;
        internal static Color noColor = Color.LightSalmon;
        internal static Color yesColor = Color.LightGreen;
        internal static Color maybeColor = Color.Yellow;

        internal static void GenerateTileTexture(int tile)
        {
            if (tileTextures.ContainsKey(tile) && tileTextures[tile] != null) return;

            try
            {
                if (tile < 0 || tile >= TextureAssets.Tile.Length)
                {
                    tileTextures[tile] = TextureAssets.MagicPixel.Value;
                    return;
                }

                Main.instance.LoadTiles(tile);
                TileObjectData tileData = TileObjectData.GetTileData(tile, 0, 0);
                if (tileData == null)
                {
                    tileTextures[tile] = TextureAssets.MagicPixel.Value;
                    return;
                }

                int width = tileData.Width;
                int height = tileData.Height;
                int coordinatePadding = tileData.CoordinatePadding;

                if (width <= 0 || height <= 0 || Main.graphics?.GraphicsDevice == null)
                {
                    tileTextures[tile] = TextureAssets.MagicPixel.Value;
                    return;
                }

                var tileAsset = TextureAssets.Tile[tile];
                if (tileAsset == null || !tileAsset.IsLoaded || tileAsset.Value == null)
                {
                    tileTextures[tile] = TextureAssets.MagicPixel.Value;
                    return;
                }

                Texture2D sourceTex = tileAsset.Value;
                int srcW = sourceTex.Width;
                int srcH = sourceTex.Height;
                Color[] srcPixels = new Color[srcW * srcH];
                sourceTex.GetData(srcPixels);

                int dstW = width * 16;
                int dstH = height * 16;
                Color[] dstPixels = new Color[dstW * dstH];

                for (int i = 0; i < width; i++)
                {
                    for (int j = 0; j < height; j++)
                    {
                        int srcStartX = i * 16 + i * coordinatePadding;
                        int srcStartY = j * 16 + j * coordinatePadding;
                        int dstStartX = i * 16;
                        int dstStartY = j * 16;

                        for (int py = 0; py < 16; py++)
                        {
                            int srcY = srcStartY + py;
                            int dstY = dstStartY + py;
                            if (srcY >= srcH || dstY >= dstH) continue;

                            for (int px = 0; px < 16; px++)
                            {
                                int srcX = srcStartX + px;
                                int dstX = dstStartX + px;
                                if (srcX >= srcW || dstX >= dstW) continue;

                                dstPixels[dstY * dstW + dstX] = srcPixels[srcY * srcW + srcX];
                            }
                        }
                    }
                }

                Texture2D result = new Texture2D(Main.instance.GraphicsDevice, dstW, dstH);
                result.SetData(dstPixels);
                tileTextures[tile] = result;
            }
            catch
            {
                tileTextures[tile] = TextureAssets.MagicPixel?.Value;
            }
        }

        internal static Texture2D GetTileImage(int tile)
        {
            if (!tileTextures.ContainsKey(tile))
            {
                GenerateTileTexture(tile);
            }
            return tileTextures.TryGetValue(tile, out var tex) ? tex : TextureAssets.MagicPixel.Value;
        }

        internal static string GetTileName(int tile)
        {
            int requiredTileStyle = Recipe.GetRequiredTileStyle(tile);
            string text = Lang.GetMapObjectName(MapHelper.TileToLookup(tile, requiredTileStyle));
            if (string.IsNullOrEmpty(text))
            {
                text = (tile < TileID.Search.Count) ? TileID.Search.GetName(tile) : $"Tile_{tile}";
            }
            return text;
        }

        internal static List<int> PopulateAdjTilesForTile(int Tile)
        {
            List<int> list = new List<int> { Tile };

            if (Tile == 302) list.Add(17);
            if (Tile == 77) list.Add(17);
            if (Tile == 133) { list.Add(17); list.Add(77); }
            if (Tile == 134) list.Add(16);
            if (Tile == 354) list.Add(14);
            if (Tile == 469) list.Add(14);
            if (Tile == 487) list.Add(14);
            if (Tile == 355) { list.Add(13); list.Add(14); }
            if (Tile == 106) list.Add(18);
            if (Tile == 114) list.Add(18);

            return list;
        }

        internal static void LoadItem(int type)
        {
            if (type > 0 && type < TextureAssets.Item.Length)
            {
                Main.instance.LoadItem(type);
            }
        }

        internal static void LoadNPC(int type)
        {
            if (type > 0 && type < TextureAssets.Npc.Length)
            {
                Main.instance.LoadNPC(type);
            }
        }
    }
}
