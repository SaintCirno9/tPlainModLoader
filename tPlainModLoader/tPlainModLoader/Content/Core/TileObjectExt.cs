using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.ID;
using Terraria.ObjectData;

namespace TPML.Content
{
    /// <summary>
    /// TileObject 扩展工具类，提供 100% 对齐 tModLoader 官方的 <c>CanPlace(..., checkStay)</c> 扩展算法。
    /// 允许在保留完整锚点支撑（地面/墙壁/天花板/侧墙）判定的同时，安全检查已存在的多方块物块驻留有效性。
    /// 作者: SaintCirno9
    /// </summary>
    public static class TileObjectExt
    {
        /// <summary>
        /// 100% 对齐 tModLoader 的 <c>TileObject.CanPlace</c> 扩展实现（支持 checkStay 参数）
        /// </summary>
        public static bool CanPlace(int x, int y, int type, int style, int dir, out TileObject objectData, bool onlyCheck = false, int? forcedRandom = null, bool checkStay = false)
        {
            TileObjectData tileData = TileObjectData.GetTileData(type, style);
            objectData = TileObject.Empty;
            if (tileData == null)
            {
                return false;
            }
            int num = x - tileData.Origin.X;
            int num2 = y - tileData.Origin.Y;
            if (num < 0 || num + tileData.Width >= Main.maxTilesX || num2 < 0 || num2 + tileData.Height >= Main.maxTilesY)
            {
                return false;
            }
            bool flag = tileData.RandomStyleRange > 0;
            if (TileObjectPreviewData.placementCache == null)
            {
                TileObjectPreviewData.placementCache = new TileObjectPreviewData();
            }
            TileObjectPreviewData.placementCache.Reset();
            int num3 = 0;
            if (tileData.AlternatesCount != 0)
            {
                num3 = tileData.AlternatesCount;
            }
            float num4 = -1f;
            float num5 = -1f;
            int num6 = 0;
            TileObjectData tileObjectData = null;
            int num7 = -1;
            Rectangle val = default(Rectangle);
            while (num7 < num3)
            {
                num7++;
                TileObjectData tileData2 = TileObjectData.GetTileData(type, style, num7);
                if (tileData2.Direction != TileObjectDirection.None && ((tileData2.Direction == TileObjectDirection.PlaceLeft && dir == 1) || (tileData2.Direction == TileObjectDirection.PlaceRight && dir == -1)))
                {
                    continue;
                }
                int num8 = x - tileData2.Origin.X;
                int num9 = y - tileData2.Origin.Y;
                if (num8 < 5 || num8 + tileData2.Width > Main.maxTilesX - 5 || num9 < 5 || num9 + tileData2.Height > Main.maxTilesY - 5)
                {
                    return false;
                }
                val = new Rectangle(0, 0, tileData2.Width, tileData2.Height);
                int num10 = 0;
                int num11 = 0;
                if (tileData2.AnchorTop.tileCount != 0)
                {
                    if (val.Y == 0)
                    {
                        val.Y = -1;
                        val.Height++;
                        num11++;
                    }
                    int checkStart = tileData2.AnchorTop.checkStart;
                    if (checkStart < val.X)
                    {
                        val.Width += val.X - checkStart;
                        num10 += val.X - checkStart;
                        val.X = checkStart;
                    }
                    int num12 = checkStart + tileData2.AnchorTop.tileCount - 1;
                    int num13 = val.X + val.Width - 1;
                    if (num12 > num13)
                    {
                        val.Width += num12 - num13;
                    }
                }
                if (tileData2.AnchorBottom.tileCount != 0)
                {
                    if (val.Y + val.Height == tileData2.Height)
                    {
                        val.Height++;
                    }
                    int checkStart2 = tileData2.AnchorBottom.checkStart;
                    if (checkStart2 < val.X)
                    {
                        val.Width += val.X - checkStart2;
                        num10 += val.X - checkStart2;
                        val.X = checkStart2;
                    }
                    int num14 = checkStart2 + tileData2.AnchorBottom.tileCount - 1;
                    int num15 = val.X + val.Width - 1;
                    if (num14 > num15)
                    {
                        val.Width += num14 - num15;
                    }
                }
                if (tileData2.AnchorLeft.tileCount != 0)
                {
                    if (val.X == 0)
                    {
                        val.X = -1;
                        val.Width++;
                        num10++;
                    }
                    int num16 = tileData2.AnchorLeft.checkStart;
                    if ((tileData2.AnchorLeft.type & AnchorType.Tree) == AnchorType.Tree)
                    {
                        num16--;
                    }
                    if (num16 < val.Y)
                    {
                        val.Height += val.Y - num16;
                        num11 += val.Y - num16;
                        val.Y = num16;
                    }
                    int num17 = num16 + tileData2.AnchorLeft.tileCount - 1;
                    if ((tileData2.AnchorLeft.type & AnchorType.Tree) == AnchorType.Tree)
                    {
                        num17 += 2;
                    }
                    int num18 = val.Y + val.Height - 1;
                    if (num17 > num18)
                    {
                        val.Height += num17 - num18;
                    }
                }
                if (tileData2.AnchorRight.tileCount != 0)
                {
                    if (val.X + val.Width == tileData2.Width)
                    {
                        val.Width++;
                    }
                    int num19 = tileData2.AnchorRight.checkStart;
                    if ((tileData2.AnchorRight.type & AnchorType.Tree) == AnchorType.Tree)
                    {
                        num19--;
                    }
                    if (num19 < val.Y)
                    {
                        val.Height += val.Y - num19;
                        num11 += val.Y - num19;
                        val.Y = num19;
                    }
                    int num20 = num19 + tileData2.AnchorRight.tileCount - 1;
                    if ((tileData2.AnchorRight.type & AnchorType.Tree) == AnchorType.Tree)
                    {
                        num20 += 2;
                    }
                    int num21 = val.Y + val.Height - 1;
                    if (num20 > num21)
                    {
                        val.Height += num20 - num21;
                    }
                }
                if (onlyCheck)
                {
                    TileObject.objectPreview.Reset();
                    TileObject.objectPreview.Active = true;
                    TileObject.objectPreview.Type = (ushort)type;
                    TileObject.objectPreview.Style = (short)style;
                    TileObject.objectPreview.Alternate = num7;
                    TileObject.objectPreview.Size = new Point16(val.Width, val.Height);
                    TileObject.objectPreview.ObjectStart = new Point16(num10, num11);
                    TileObject.objectPreview.Coordinates = new Point16(num8 - num10, num9 - num11);
                }
                float num22 = 0f;
                float num23 = tileData2.Width * tileData2.Height;
                float num24 = 0f;
                float num25 = 0f;
                for (int i = 0; i < tileData2.Width; i++)
                {
                    for (int j = 0; j < tileData2.Height; j++)
                    {
                        Tile tileSafely = Framing.GetTileSafely(num8 + i, num9 + j);
                        bool flag2 = !tileData2.LiquidPlace(tileSafely);
                        bool flag3 = false;
                        if (tileData2.AnchorWall)
                        {
                            num25++;
                            if (!tileData2.isValidWallAnchor(tileSafely.wall))
                            {
                                flag3 = true;
                            }
                            else
                            {
                                num24++;
                            }
                        }
                        bool flag4 = false;
                        if (tileSafely.active() && (!Main.tileCut[tileSafely.type] || tileSafely.type == 484 || tileSafely.type == 654) && !TileID.Sets.BreakableWhenPlacing[tileSafely.type] && !checkStay)
                        {
                            flag4 = true;
                        }
                        if (flag4 | flag2 | flag3)
                        {
                            if (onlyCheck)
                            {
                                TileObject.objectPreview[i + num10, j + num11] = 2;
                            }
                            continue;
                        }
                        if (onlyCheck)
                        {
                            TileObject.objectPreview[i + num10, j + num11] = 1;
                        }
                        num22++;
                    }
                }
                AnchorData anchorBottom = tileData2.AnchorBottom;
                if (anchorBottom.tileCount != 0)
                {
                    num25 += (float)anchorBottom.tileCount;
                    int height = tileData2.Height;
                    for (int k = 0; k < anchorBottom.tileCount; k++)
                    {
                        int num26 = anchorBottom.checkStart + k;
                        Tile tileSafely2 = Framing.GetTileSafely(num8 + num26, num9 + height);
                        bool flag5 = false;
                        if (tileSafely2.nactive())
                        {
                            if ((anchorBottom.type & AnchorType.SolidTile) == AnchorType.SolidTile && Main.tileSolid[tileSafely2.type] && !Main.tileSolidTop[tileSafely2.type] && !Main.tileNoAttach[tileSafely2.type] && (tileData2.FlattenAnchors || tileSafely2.blockType() == 0))
                            {
                                flag5 = tileData2.isValidTileAnchor(tileSafely2.type);
                            }
                            if (!flag5 && ((anchorBottom.type & AnchorType.SolidWithTop) == AnchorType.SolidWithTop || (anchorBottom.type & AnchorType.Table) == AnchorType.Table))
                            {
                                if (TileID.Sets.Platforms[tileSafely2.type])
                                {
                                    _ = tileSafely2.frameX / TileObjectData.PlatformFrameWidth();
                                    if (!tileSafely2.halfBrick() && WorldGen.PlatformProperTopFrame(tileSafely2.frameX))
                                    {
                                        flag5 = true;
                                    }
                                }
                                else if (Main.tileSolid[tileSafely2.type] && Main.tileSolidTop[tileSafely2.type])
                                {
                                    flag5 = true;
                                }
                            }
                            if (!flag5 && (anchorBottom.type & AnchorType.Table) == AnchorType.Table && !TileID.Sets.Platforms[tileSafely2.type] && Main.tileTable[tileSafely2.type] && tileSafely2.blockType() == 0)
                            {
                                flag5 = true;
                            }
                            if (!flag5 && (anchorBottom.type & AnchorType.SolidSide) == AnchorType.SolidSide && Main.tileSolid[tileSafely2.type] && !Main.tileSolidTop[tileSafely2.type] && (uint)(tileSafely2.blockType() - 4) <= 1u)
                            {
                                flag5 = tileData2.isValidTileAnchor(tileSafely2.type);
                            }
                            if (!flag5 && (anchorBottom.type & AnchorType.AlternateTile) == AnchorType.AlternateTile && tileData2.isValidAlternateAnchor(tileSafely2.type))
                            {
                                flag5 = true;
                            }
                        }
                        else if (!flag5 && (anchorBottom.type & AnchorType.EmptyTile) == AnchorType.EmptyTile)
                        {
                            flag5 = true;
                        }
                        if (!flag5)
                        {
                            if (onlyCheck)
                            {
                                TileObject.objectPreview[num26 + num10, height + num11] = 2;
                            }
                            continue;
                        }
                        if (onlyCheck)
                        {
                            TileObject.objectPreview[num26 + num10, height + num11] = 1;
                        }
                        num24++;
                    }
                }
                anchorBottom = tileData2.AnchorTop;
                if (anchorBottom.tileCount != 0)
                {
                    num25 += (float)anchorBottom.tileCount;
                    int num27 = -1;
                    for (int l = 0; l < anchorBottom.tileCount; l++)
                    {
                        int num28 = anchorBottom.checkStart + l;
                        Tile tileSafely3 = Framing.GetTileSafely(num8 + num28, num9 + num27);
                        bool flag6 = false;
                        if (tileSafely3.nactive())
                        {
                            if ((anchorBottom.type & AnchorType.SolidTile) == AnchorType.SolidTile && Main.tileSolid[tileSafely3.type] && !Main.tileSolidTop[tileSafely3.type] && !Main.tileNoAttach[tileSafely3.type] && (tileData2.FlattenAnchors || tileSafely3.blockType() == 0))
                            {
                                flag6 = tileData2.isValidTileAnchor(tileSafely3.type);
                            }
                            if (!flag6 && (anchorBottom.type & AnchorType.SolidBottom) == AnchorType.SolidBottom && ((Main.tileSolid[tileSafely3.type] && (!Main.tileSolidTop[tileSafely3.type] || (TileID.Sets.Platforms[tileSafely3.type] && (tileSafely3.halfBrick() || tileSafely3.topSlope())))) || tileSafely3.halfBrick() || tileSafely3.topSlope()) && !TileID.Sets.NotReallySolid[tileSafely3.type] && !tileSafely3.bottomSlope())
                            {
                                flag6 = tileData2.isValidTileAnchor(tileSafely3.type);
                            }
                            if (!flag6 && (anchorBottom.type & AnchorType.Platform) == AnchorType.Platform && TileID.Sets.Platforms[tileSafely3.type])
                            {
                                flag6 = tileData2.isValidTileAnchor(tileSafely3.type);
                            }
                            if (!flag6 && (anchorBottom.type & AnchorType.PlatformNonHammered) == AnchorType.PlatformNonHammered && TileID.Sets.Platforms[tileSafely3.type] && tileSafely3.slope() == 0 && !tileSafely3.halfBrick())
                            {
                                flag6 = tileData2.isValidTileAnchor(tileSafely3.type);
                            }
                            if (!flag6 && (anchorBottom.type & AnchorType.PlanterBox) == AnchorType.PlanterBox && tileSafely3.type == 380)
                            {
                                flag6 = tileData2.isValidTileAnchor(tileSafely3.type);
                            }
                            if (!flag6 && (anchorBottom.type & AnchorType.SolidSide) == AnchorType.SolidSide && Main.tileSolid[tileSafely3.type] && !Main.tileSolidTop[tileSafely3.type] && (uint)(tileSafely3.blockType() - 2) <= 1u)
                            {
                                flag6 = tileData2.isValidTileAnchor(tileSafely3.type);
                            }
                            if (!flag6 && (anchorBottom.type & AnchorType.AlternateTile) == AnchorType.AlternateTile && tileData2.isValidAlternateAnchor(tileSafely3.type))
                            {
                                flag6 = true;
                            }
                        }
                        else if (!flag6 && (anchorBottom.type & AnchorType.EmptyTile) == AnchorType.EmptyTile)
                        {
                            flag6 = true;
                        }
                        if (!flag6)
                        {
                            if (onlyCheck)
                            {
                                TileObject.objectPreview[num28 + num10, num27 + num11] = 2;
                            }
                            continue;
                        }
                        if (onlyCheck)
                        {
                            TileObject.objectPreview[num28 + num10, num27 + num11] = 1;
                        }
                        num24++;
                    }
                }
                anchorBottom = tileData2.AnchorRight;
                if (anchorBottom.tileCount != 0)
                {
                    num25 += (float)anchorBottom.tileCount;
                    int width = tileData2.Width;
                    for (int m = 0; m < anchorBottom.tileCount; m++)
                    {
                        int num29 = anchorBottom.checkStart + m;
                        Tile tileSafely4 = Framing.GetTileSafely(num8 + width, num9 + num29);
                        bool flag7 = false;
                        if (tileSafely4.nactive())
                        {
                            if ((anchorBottom.type & AnchorType.SolidTile) == AnchorType.SolidTile && Main.tileSolid[tileSafely4.type] && !Main.tileSolidTop[tileSafely4.type] && !Main.tileNoAttach[tileSafely4.type] && (tileData2.FlattenAnchors || tileSafely4.blockType() == 0))
                            {
                                flag7 = tileData2.isValidTileAnchor(tileSafely4.type);
                            }
                            if (!flag7 && (anchorBottom.type & AnchorType.SolidSide) == AnchorType.SolidSide && Main.tileSolid[tileSafely4.type] && !Main.tileSolidTop[tileSafely4.type])
                            {
                                int num30 = tileSafely4.blockType();
                                if (num30 == 2 || num30 == 4)
                                {
                                    flag7 = tileData2.isValidTileAnchor(tileSafely4.type);
                                }
                            }
                            if (!flag7 && (anchorBottom.type & AnchorType.Tree) == AnchorType.Tree && TileID.Sets.IsATreeTrunk[tileSafely4.type])
                            {
                                flag7 = true;
                                if (m == 0)
                                {
                                    num25++;
                                    Tile tileSafely5 = Framing.GetTileSafely(num8 + width, num9 + num29 - 1);
                                    if (tileSafely5.nactive() && TileID.Sets.IsATreeTrunk[tileSafely5.type])
                                    {
                                        num24++;
                                        if (onlyCheck)
                                        {
                                            TileObject.objectPreview[width + num10, num29 + num11 - 1] = 1;
                                        }
                                    }
                                    else if (onlyCheck)
                                    {
                                        TileObject.objectPreview[width + num10, num29 + num11 - 1] = 2;
                                    }
                                }
                                if (m == anchorBottom.tileCount - 1)
                                {
                                    num25++;
                                    Tile tileSafely6 = Framing.GetTileSafely(num8 + width, num9 + num29 + 1);
                                    if (tileSafely6.nactive() && TileID.Sets.IsATreeTrunk[tileSafely6.type])
                                    {
                                        num24++;
                                        if (onlyCheck)
                                        {
                                            TileObject.objectPreview[width + num10, num29 + num11 + 1] = 1;
                                        }
                                    }
                                    else if (onlyCheck)
                                    {
                                        TileObject.objectPreview[width + num10, num29 + num11 + 1] = 2;
                                    }
                                }
                            }
                            if (!flag7 && (anchorBottom.type & AnchorType.AlternateTile) == AnchorType.AlternateTile && tileData2.isValidAlternateAnchor(tileSafely4.type))
                            {
                                flag7 = true;
                            }
                        }
                        else if (!flag7 && (anchorBottom.type & AnchorType.EmptyTile) == AnchorType.EmptyTile)
                        {
                            flag7 = true;
                        }
                        if (!flag7)
                        {
                            if (onlyCheck)
                            {
                                TileObject.objectPreview[width + num10, num29 + num11] = 2;
                            }
                            continue;
                        }
                        if (onlyCheck)
                        {
                            TileObject.objectPreview[width + num10, num29 + num11] = 1;
                        }
                        num24++;
                    }
                }
                anchorBottom = tileData2.AnchorLeft;
                if (anchorBottom.tileCount != 0)
                {
                    num25 += (float)anchorBottom.tileCount;
                    int num31 = -1;
                    for (int n = 0; n < anchorBottom.tileCount; n++)
                    {
                        int num32 = anchorBottom.checkStart + n;
                        Tile tileSafely7 = Framing.GetTileSafely(num8 + num31, num9 + num32);
                        bool flag8 = false;
                        if (tileSafely7.nactive())
                        {
                            if ((anchorBottom.type & AnchorType.SolidTile) == AnchorType.SolidTile && Main.tileSolid[tileSafely7.type] && !Main.tileSolidTop[tileSafely7.type] && !Main.tileNoAttach[tileSafely7.type] && (tileData2.FlattenAnchors || tileSafely7.blockType() == 0))
                            {
                                flag8 = tileData2.isValidTileAnchor(tileSafely7.type);
                            }
                            if (!flag8 && (anchorBottom.type & AnchorType.SolidSide) == AnchorType.SolidSide && Main.tileSolid[tileSafely7.type] && !Main.tileSolidTop[tileSafely7.type])
                            {
                                int num33 = tileSafely7.blockType();
                                if (num33 == 3 || num33 == 5)
                                {
                                    flag8 = tileData2.isValidTileAnchor(tileSafely7.type);
                                }
                            }
                            if (!flag8 && (anchorBottom.type & AnchorType.Tree) == AnchorType.Tree && TileID.Sets.IsATreeTrunk[tileSafely7.type])
                            {
                                flag8 = true;
                                if (n == 0)
                                {
                                    num25++;
                                    Tile tileSafely8 = Framing.GetTileSafely(num8 + num31, num9 + num32 - 1);
                                    if (tileSafely8.nactive() && TileID.Sets.IsATreeTrunk[tileSafely8.type])
                                    {
                                        num24++;
                                        if (onlyCheck)
                                        {
                                            TileObject.objectPreview[num31 + num10, num32 + num11 - 1] = 1;
                                        }
                                    }
                                    else if (onlyCheck)
                                    {
                                        TileObject.objectPreview[num31 + num10, num32 + num11 - 1] = 2;
                                    }
                                }
                                if (n == anchorBottom.tileCount - 1)
                                {
                                    num25++;
                                    Tile tileSafely9 = Framing.GetTileSafely(num8 + num31, num9 + num32 + 1);
                                    if (tileSafely9.nactive() && TileID.Sets.IsATreeTrunk[tileSafely9.type])
                                    {
                                        num24++;
                                        if (onlyCheck)
                                        {
                                            TileObject.objectPreview[num31 + num10, num32 + num11 + 1] = 1;
                                        }
                                    }
                                    else if (onlyCheck)
                                    {
                                        TileObject.objectPreview[num31 + num10, num32 + num11 + 1] = 2;
                                    }
                                }
                            }
                            if (!flag8 && (anchorBottom.type & AnchorType.AlternateTile) == AnchorType.AlternateTile && tileData2.isValidAlternateAnchor(tileSafely7.type))
                            {
                                flag8 = true;
                            }
                        }
                        else if (!flag8 && (anchorBottom.type & AnchorType.EmptyTile) == AnchorType.EmptyTile)
                        {
                            flag8 = true;
                        }
                        if (!flag8)
                        {
                            if (onlyCheck)
                            {
                                TileObject.objectPreview[num31 + num10, num32 + num11] = 2;
                            }
                            continue;
                        }
                        if (onlyCheck)
                        {
                            TileObject.objectPreview[num31 + num10, num32 + num11] = 1;
                        }
                        num24++;
                    }
                }
                if (tileData2.HookCheckIfCanPlace.hook != null)
                {
                    if (tileData2.HookCheckIfCanPlace.processedCoordinates)
                    {
                        _ = tileData2.Origin;
                        _ = tileData2.Origin;
                    }
                    if (tileData2.HookCheckIfCanPlace.hook(x, y, type, style, dir, num7) == tileData2.HookCheckIfCanPlace.badReturn && tileData2.HookCheckIfCanPlace.badResponse == 0)
                    {
                        num24 = 0f;
                        num22 = 0f;
                        TileObject.objectPreview.AllInvalid();
                    }
                }
                float num34 = num24 / num25;
                if (num25 == 0f)
                {
                    num34 = 1f;
                }
                float num35 = num22 / num23;
                if (num35 == 1f && num25 == 0f)
                {
                    num23 = 1f;
                    num25 = 1f;
                    num34 = 1f;
                    num35 = 1f;
                }
                if (num34 == 1f && num35 == 1f)
                {
                    num4 = 1f;
                    num5 = 1f;
                    num6 = num7;
                    tileObjectData = tileData2;
                    break;
                }
                if (num34 > num4 || (num34 == num4 && num35 > num5))
                {
                    TileObjectPreviewData.placementCache.CopyFrom(TileObject.objectPreview);
                    num4 = num34;
                    num5 = num35;
                    tileObjectData = tileData2;
                    num6 = num7;
                }
            }
            int num36 = -1;
            if (flag)
            {
                if (TileObjectPreviewData.randomCache == null)
                {
                    TileObjectPreviewData.randomCache = new TileObjectPreviewData();
                }
                bool flag9 = false;
                if (TileObjectPreviewData.randomCache.Type == type)
                {
                    Point16 coordinates = TileObjectPreviewData.randomCache.Coordinates;
                    Point16 objectStart = TileObjectPreviewData.randomCache.ObjectStart;
                    int num37 = coordinates.X + objectStart.X;
                    int num38 = coordinates.Y + objectStart.Y;
                    int num39 = x - (tileObjectData?.Origin.X ?? 0);
                    int num40 = y - (tileObjectData?.Origin.Y ?? 0);
                    if (num37 != num39 || num38 != num40)
                    {
                        flag9 = true;
                    }
                }
                else
                {
                    flag9 = true;
                }
                int randomStyleRange = tileData.RandomStyleRange;
                int num41 = Main.rand.Next(tileData.RandomStyleRange);
                if (forcedRandom.HasValue)
                {
                    num41 = (forcedRandom.Value % randomStyleRange + randomStyleRange) % randomStyleRange;
                }
                num36 = ((!flag9 && !forcedRandom.HasValue) ? TileObjectPreviewData.randomCache.Random : num41);
            }
            if (tileData.SpecificRandomStyles != null)
            {
                if (TileObjectPreviewData.randomCache == null)
                {
                    TileObjectPreviewData.randomCache = new TileObjectPreviewData();
                }
                bool flag10 = false;
                if (TileObjectPreviewData.randomCache.Type == type)
                {
                    Point16 coordinates2 = TileObjectPreviewData.randomCache.Coordinates;
                    Point16 objectStart2 = TileObjectPreviewData.randomCache.ObjectStart;
                    int num42 = coordinates2.X + objectStart2.X;
                    int num43 = coordinates2.Y + objectStart2.Y;
                    int num44 = x - tileData.Origin.X;
                    int num45 = y - tileData.Origin.Y;
                    if (num42 != num44 || num43 != num45)
                    {
                        flag10 = true;
                    }
                }
                else
                {
                    flag10 = true;
                }
                int num46 = tileData.SpecificRandomStyles.Length;
                int num47 = Main.rand.Next(num46);
                if (forcedRandom.HasValue)
                {
                    num47 = (forcedRandom.Value % num46 + num46) % num46;
                }
                num36 = ((!flag10 && !forcedRandom.HasValue) ? TileObjectPreviewData.randomCache.Random : (tileData.SpecificRandomStyles[num47] - style));
            }
            if (onlyCheck)
            {
                if (num4 != 1f || num5 != 1f)
                {
                    TileObject.objectPreview.CopyFrom(TileObjectPreviewData.placementCache);
                    num7 = num6;
                }
                TileObject.objectPreview.Random = num36;
                if (tileData.RandomStyleRange > 0 || tileData.SpecificRandomStyles != null)
                {
                    TileObjectPreviewData.randomCache.CopyFrom(TileObject.objectPreview);
                }
            }
            if (!onlyCheck)
            {
                objectData.xCoord = x - (tileObjectData?.Origin.X ?? 0);
                objectData.yCoord = y - (tileObjectData?.Origin.Y ?? 0);
                objectData.type = type;
                objectData.style = style;
                objectData.alternate = num7;
                objectData.random = num36;
            }
            if (num4 == 1f)
            {
                return num5 == 1f;
            }
            return false;
        }
    }
}
