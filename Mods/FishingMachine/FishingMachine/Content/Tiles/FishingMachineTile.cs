using FishingMachine.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.ID;
using Terraria.ObjectData;
using TPML.Content;

namespace FishingMachine.Content.Tiles
{
    /// <summary>
    /// 自动钓鱼机 2x2 多方块建筑 (ModTile)
    /// 继承 TPML 原生 ModTile 规范，具备原生 2x2 绿色框放置、破坏掉落与右键交互生命周期
    /// 作者: SaintCirno9
    /// </summary>
    public class FishingMachineTile : ModTile
    {
        public static Texture2D HighlightTexture;

        public override string Texture => "FishingMachine/Resources/AutofisherTile";

        public override void SetStaticDefaults()
        {
            Main.tileFrameImportant[Type] = true;
            Main.tileLavaDeath[Type] = false;
            Main.tileNoAttach[Type] = true;
            TileID.Sets.DisableSmartCursor[Type] = true;

            TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
            TileObjectData.newTile.Origin = new Point16(1, 1);
            TileObjectData.newTile.CoordinateHeights = new int[] { 16, 16 };
            TileObjectData.newTile.CoordinateWidth = 16;
            TileObjectData.newTile.CoordinatePadding = 2;
            TileObjectData.newTile.Direction = TileObjectDirection.PlaceRight;
            TileObjectData.newTile.DrawYOffset = 2;
            TileObjectData.newTile.StyleHorizontal = true;
            TileObjectData.newTile.HookPostPlaceMyPlayer = new PlacementHook(
                TEFishingMachine.Hook_AfterPlacement, -1, 0, false);

            TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile);
            TileObjectData.newAlternate.Direction = TileObjectDirection.PlaceLeft;
            TileObjectData.addAlternate(1);

            TileObjectData.addTile(Type);

            ItemDrop = ModContent.ItemType<Items.FishingMachine>();
            DustType = DustID.Iron;
        }

        public override bool RightClick(int i, int j)
        {
            Tile tile = Framing.GetTileSafely(i, j);
            int originX = i - (tile.frameX % 36) / 18;
            int originY = j - (tile.frameY % 36) / 18;

            int id = ModContent.GetInstance<TEFishingMachine>().Find(originX, originY);
            if (id >= 0 && TileEntity.ByID.TryGetValue(id, out var te) && te is TEFishingMachine machine)
            {
                FishingMachineUI.Toggle(machine);
                return true;
            }
            return false;
        }

        public override void MouseOver(int i, int j)
        {
            Player local = Main.LocalPlayer;
            local.cursorItemIconEnabled = true;
            local.cursorItemIconID = ModContent.ItemType<Items.FishingMachine>();
            local.cursorItemIconText = "自动钓鱼机\n右键打开交互界面";
        }

        public override void KillMultiTile(int i, int j, int frameX, int frameY)
        {
            int id = ModContent.GetInstance<TEFishingMachine>().Find(i, j);
            if (id >= 0 && TileEntity.ByID.TryGetValue(id, out var te) && te is TEFishingMachine machine)
            {
                machine.DropContents();
                if (FishingMachineUI.CurrentEntity == machine)
                {
                    FishingMachineUI.Close();
                }
            }
        }

        public override void PostDraw(int i, int j, SpriteBatch spriteBatch)
        {
            Tile tile = Framing.GetTileSafely(i, j);
            // 仅在左上角图格 (frameX == 0 && frameY == 0) 进行一次性整机高亮判断
            if (tile.frameX % 36 == 0 && tile.frameY % 36 == 0 && HighlightTexture != null)
            {
                Vector2 mouseWorld = Main.MouseWorld;
                bool hover = mouseWorld.X >= i * 16f && mouseWorld.X < (i + 2) * 16f &&
                             mouseWorld.Y >= j * 16f && mouseWorld.Y < (j + 2) * 16f;

                if (hover && !Main.playerInventory && !FishingMachineUI.IsVisible && !FishingMachineUI.SelectPoolMode)
                {
                    Vector2 screenPos = Main.screenPosition;
                    float drawPosX = i * 16f - screenPos.X;
                    float drawPosY = j * 16f - screenPos.Y;
                    Rectangle source = new Rectangle(0, 0, 36, 36);
                    spriteBatch.Draw(HighlightTexture, new Rectangle((int)drawPosX, (int)drawPosY, 36, 36), source, Color.White * 0.85f);
                }
            }
        }
    }
}