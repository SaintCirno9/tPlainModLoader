using FishingMachine.UI;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using TPML.Content;

namespace FishingMachine.Content.Tiles
{
    /// <summary>
    /// 自动钓鱼机 2x2 物块管理器、绘制与物块挖掘破坏拦截
    /// 作者: SaintCirno9
    /// </summary>
    public static class FishingMachineTileManager
    {
        public static Dictionary<Point16, TEFishingMachine> ActiveEntities = new Dictionary<Point16, TEFishingMachine>();
        public static Texture2D TileTexture;
        public static Texture2D HighlightTexture;

        public static void ClearAll()
        {
            ActiveEntities.Clear();
        }

        public static bool GetMachineAt(int x, int y, out TEFishingMachine machine, out Point16 origin)
        {
            foreach (var kv in ActiveEntities)
            {
                Point16 pos = kv.Key;
                if (x >= pos.X && x <= pos.X + 1 && y >= pos.Y && y <= pos.Y + 1)
                {
                    machine = kv.Value;
                    origin = pos;
                    return true;
                }
            }
            machine = null;
            origin = Point16.NegativeOne;
            return false;
        }

        public static bool CanPlace(int x, int y)
        {
            if (x < 10 || x >= Main.maxTilesX - 10 || y < 10 || y >= Main.maxTilesY - 10) return false;

            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    Tile t = Framing.GetTileSafely(x + i, y + j);
                    if (t.active() && (Main.tileSolid[t.type] || Main.tileSolidTop[t.type])) return false;
                }
            }

            Tile ground1 = Framing.GetTileSafely(x, y + 2);
            Tile ground2 = Framing.GetTileSafely(x + 1, y + 2);
            return (ground1.active() && (Main.tileSolid[ground1.type] || Main.tileSolidTop[ground1.type])) &&
                   (ground2.active() && (Main.tileSolid[ground2.type] || Main.tileSolidTop[ground2.type]));
        }

        public static void Place(int x, int y)
        {
            Point16 pos = new Point16(x, y);
            TEFishingMachine entity = new TEFishingMachine(pos);
            entity.FindNearbyWater();
            ActiveEntities[pos] = entity;

            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    Tile t = Framing.GetTileSafely(x + i, y + j);
                    t.active(true);
                    t.type = TileID.Anvils;
                    t.frameX = 0;
                    t.frameY = 0;
                }
            }

            WorldGen.SquareTileFrame(x, y);
            NetMessage.SendTileSquare(-1, x, y, 2, 2);
            Main.NewText("[c/00FFDD:自动钓鱼机放置成功！右键点击机器可打开专属交互面板。]");
        }

        public static void DestroyMachine(Point16 origin)
        {
            if (ActiveEntities.TryGetValue(origin, out TEFishingMachine entity))
            {
                ActiveEntities.Remove(origin);

                // 掉落内部存储所有物品
                entity?.DropContents();

                // 掉落机器物品本身
                int itemType = ModContent.ItemType<Items.FishingMachine>();
                if (itemType > 0)
                {
                    IEntitySource src = new EntitySource_TileBreak(origin.X, origin.Y);
                    Item.NewItem(src, new Vector2(origin.X * 16 + 16, origin.Y * 16 + 16), itemType, 1);
                }

                // 关闭 UI
                if (FishingMachineUI.CurrentEntity == entity)
                {
                    FishingMachineUI.Close();
                }

                // 清理 2x2 占位物块
                ClearStructure(origin);
            }
        }

        public static void UpdateAll()
        {
            List<Point16> toRemove = null;
            foreach (var kv in ActiveEntities)
            {
                Point16 pos = kv.Key;
                TEFishingMachine entity = kv.Value;

                if (!IsStructureIntact(pos))
                {
                    if (toRemove == null) toRemove = new List<Point16>();
                    toRemove.Add(pos);
                    continue;
                }

                entity.Update();
            }

            if (toRemove != null)
            {
                foreach (var pos in toRemove)
                {
                    DestroyMachine(pos);
                }
            }
        }

        private static bool IsStructureIntact(Point16 pos)
        {
            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    Tile t = Framing.GetTileSafely(pos.X + i, pos.Y + j);
                    if (!t.active()) return false;
                }
            }
            return true;
        }

        public static void ClearStructure(Point16 pos)
        {
            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    Tile t = Framing.GetTileSafely(pos.X + i, pos.Y + j);
                    t.active(false);
                    t.frameX = 0;
                    t.frameY = 0;
                }
            }

            WorldGen.SquareTileFrame(pos.X, pos.Y);
            NetMessage.SendTileSquare(-1, pos.X, pos.Y, 2, 2);
        }

        public static bool CheckRightClick(int tileX, int tileY)
        {
            if (GetMachineAt(tileX, tileY, out var machine, out _))
            {
                FishingMachineUI.Toggle(machine);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 在瓦片层绘制完成后绘制自动钓鱼机本体
        /// </summary>
        public static void DrawAll(SpriteBatch sb)
        {
            if (TileTexture == null) return;

            Vector2 screenPos = Main.screenPosition;
            Vector2 mouseWorld = Main.MouseWorld;
            int machineItemType = ModContent.ItemType<Items.FishingMachine>();

            foreach (var kv in ActiveEntities)
            {
                Point16 pos = kv.Key;
                float drawPosX = pos.X * 16f - 2f - screenPos.X;
                float drawPosY = pos.Y * 16f - 2f - screenPos.Y;

                if (drawPosX < -72 || drawPosX > Main.screenWidth + 72 ||
                    drawPosY < -72 || drawPosY > Main.screenHeight + 72)
                {
                    continue;
                }

                Rectangle source = new Rectangle(0, 0, 36, 36);
                Color color = Lighting.GetColor(pos.X, pos.Y);
                sb.Draw(TileTexture, new Rectangle((int)drawPosX, (int)drawPosY, 36, 36), source, color);

                bool hover = mouseWorld.X >= pos.X * 16f && mouseWorld.X < pos.X * 16f + 36f &&
                             mouseWorld.Y >= pos.Y * 16f && mouseWorld.Y < pos.Y * 16f + 36f;
                if (hover)
                {
                    if (HighlightTexture != null)
                    {
                        sb.Draw(HighlightTexture, new Rectangle((int)drawPosX, (int)drawPosY, 36, 36), source, Color.White * 0.85f);
                    }

                    if (!Main.playerInventory && !FishingMachineUI.IsVisible && !FishingMachineUI.SelectPoolMode)
                    {
                        Player local = Main.LocalPlayer;
                        local.cursorItemIconEnabled = true;
                        local.cursorItemIconID = machineItemType;
                        local.cursorItemIconText = "自动钓鱼机\n右键打开交互界面";
                    }
                }
            }
        }
    }

    /// <summary>
    /// 挖掘破坏钓鱼机瓦片时的掉落与实体回收拦截
    /// </summary>
    [HarmonyPatch(typeof(WorldGen), nameof(WorldGen.KillTile))]
    public static class Patch_FishingMachineKillTile
    {
        [HarmonyPrefix]
        public static bool Prefix(int i, int j, bool fail, bool effectOnly, bool noItem)
        {
            if (!fail && !effectOnly)
            {
                if (FishingMachineTileManager.GetMachineAt(i, j, out _, out Point16 origin))
                {
                    FishingMachineTileManager.DestroyMachine(origin);
                    return false;
                }
            }
            return true;
        }
    }
}