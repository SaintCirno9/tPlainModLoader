using Microsoft.Xna.Framework.Graphics;
using System.IO;
using System.Reflection;

namespace WandsTool
{
    public class Resources
    {
        public static Texture2D Images_Wand = null;
        public static Texture2D Images_WandCreate = null;
        public static Texture2D Images_ShapesLine = null;
        public static Texture2D Images_ShapesCircular = null;
        public static Texture2D Images_ShapesFilledCircular = null;
        public static Texture2D Images_ShapesRectangle = null;
        public static Texture2D Images_ShapesHollowRectangle = null;
        public static Texture2D Images_SlopeSolid = null;
        public static Texture2D Images_SlopeHalfBlock = null;
        public static Texture2D Images_SlopeUpLeft = null;
        public static Texture2D Images_SlopeUpRight = null;
        public static Texture2D Images_SlopeDownLeft = null;
        public static Texture2D Images_SlopeDownRight = null;

        public static bool Loaded { get; private set; } = false;

        public static void Load()
        {
            if (Loaded || Terraria.Main.dedServ || Terraria.Main.graphics?.GraphicsDevice == null) return;

            Assembly assembly = Assembly.GetExecutingAssembly();
            setTexture2D(assembly, "WandsTool.Resources.Wand.png", ref Images_Wand);
            setTexture2D(assembly, "WandsTool.Resources.WandCreate.png", ref Images_WandCreate);
            setTexture2D(assembly, "WandsTool.Resources.ShapesLine.png", ref Images_ShapesLine);
            setTexture2D(assembly, "WandsTool.Resources.ShapesCircular.png", ref Images_ShapesCircular);
            setTexture2D(assembly, "WandsTool.Resources.ShapesFilledCircular.png", ref Images_ShapesFilledCircular);
            setTexture2D(assembly, "WandsTool.Resources.ShapesRectangle.png", ref Images_ShapesRectangle);
            setTexture2D(assembly, "WandsTool.Resources.ShapesHollowRectangle.png", ref Images_ShapesHollowRectangle);
            setTexture2D(assembly, "WandsTool.Resources.SlopeSolid.png", ref Images_SlopeSolid);
            setTexture2D(assembly, "WandsTool.Resources.SlopeHalfBlock.png", ref Images_SlopeHalfBlock);
            setTexture2D(assembly, "WandsTool.Resources.SlopeUpLeft.png", ref Images_SlopeUpLeft);
            setTexture2D(assembly, "WandsTool.Resources.SlopeUpRight.png", ref Images_SlopeUpRight);
            setTexture2D(assembly, "WandsTool.Resources.SlopeDownLeft.png", ref Images_SlopeDownLeft);
            setTexture2D(assembly, "WandsTool.Resources.SlopeDownRight.png", ref Images_SlopeDownRight);
            Loaded = true;
        }

        private static void setTexture2D(Assembly assembly, string s, ref Texture2D t2d)
        {
            if (Terraria.Main.graphics?.GraphicsDevice == null) return;

            using (Stream stream = assembly.GetManifestResourceStream(s))
            {
                if (stream == null) return;
                t2d = Texture2D.FromStream(Terraria.Main.graphics.GraphicsDevice, stream);
            }
        }
    }
}
