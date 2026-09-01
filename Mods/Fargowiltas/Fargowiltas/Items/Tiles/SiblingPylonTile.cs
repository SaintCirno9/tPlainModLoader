using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using TPML.Content;
using Terraria.ObjectData;
using Fargowiltas.NPCs;
using Fargowiltas.TileEntities;
using System.Linq;

namespace Fargowiltas.Items.Tiles
{
    public class SiblingPylonTile : ModTile
    {
        public const int CrystalVerticalFrameCount = 8;

        public Asset<Texture2D> crystalTexture;
        public Asset<Texture2D> crystalHighlightTexture;
        public Asset<Texture2D> mapIcon;

        public override void Load(Mod mod)
        {
            base.Load(mod);
            crystalTexture = ModContent.Request<Texture2D>(Texture + "_Crystal");
            crystalHighlightTexture = ModContent.Request<Texture2D>(Texture + "_CrystalHighlight");
            mapIcon = ModContent.Request<Texture2D>(Texture + "_MapIcon");
        }

        public override void SetStaticDefaults()
        {
            Main.tileLighted[Type] = true;
            Main.tileFrameImportant[Type] = true;

            TileObjectData.newTile.CopyFrom(TileObjectData.Style3x4);
            TileObjectData.newTile.LavaDeath = false;
            TileObjectData.newTile.DrawYOffset = 2;
            TileObjectData.newTile.StyleHorizontal = true;
            TileObjectData.newTile.HookPostPlaceMyPlayer = ModTileEntity.GetPlacementHook<SiblingPylonTileEntity>();

            TileObjectData.addTile(Type);

            // pylon set
            // pylon set

            // AddToArray(ref TileID.Sets.CountsAsPylon);

            LocalizedText pylonName = CreateMapEntryName();
            AddMapEntry(Color.White, pylonName);
        }

        public override bool RightClick(int i, int j)
        {
            Main.LocalPlayer.TryOpeningFullscreenMap();
            return true;
        }

        public override void MouseOver(int i, int j)
        {
            Main.LocalPlayer.cursorItemIconEnabled = true;
            Main.LocalPlayer.cursorItemIconID = ModContent.ItemType<SiblingPylon>();
        }

        public override void KillMultiTile(int i, int j, int frameX, int frameY)
        {
            ModContent.GetInstance<SiblingPylonTileEntity>().Kill(i, j);
        }

        public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
        {
            r = 51f / 255f * 0.75f;
            g = 255f / 255f * 0.75f;
            b = 191f / 255f * 0.75f;
        }
    }
}