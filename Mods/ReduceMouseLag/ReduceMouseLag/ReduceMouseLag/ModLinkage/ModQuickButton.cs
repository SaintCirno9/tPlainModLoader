using System;
using System.Linq;
using tContentPatch;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;

namespace ReduceMouseLag.ModLinkage
{
    public class ModQuickButton : Mod
    {
        public static bool IsLinkage = true;

        public override void Loaded()
        {
            if (Main.dedServ) return;
            if (IsLinkage == false) return;

            var mos = ContentPatch.GetModObjects();
            if (mos == null) return;

            var mo = mos.FirstOrDefault(i => i.assembly?.GetName().Name == "QuickButton");
            if (mo == null) return;

            Type type = mo.assembly.GetType("QuickButton.QuickButton.QuickButton");
            if (type == null) return;

            var mi = type.GetMethod("Add", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            if (mi == null) return;

            UIImage ui_img = new UIImage(Main.Assets.Request<Microsoft.Xna.Framework.Graphics.Texture2D>("Images/UI/Cursor_0", ReLogic.Content.AssetRequestMode.ImmediateLoad));
            ui_img.Width.Pixels = 32;
            ui_img.Height.Pixels = 32;
            ui_img.ScaleToFit = true;
            ui_img.OnUpdate += _ =>
            {
                if (ui_img.IsMouseHovering)
                {
                    Main.instance.MouseText($"鼠标延迟优化: {(ModConfig.IsEnabled ? "已开启" : "已禁用")}");
                }
            };
            ui_img.OnLeftClick += (e, s) =>
            {
                SoundEngine.PlaySound(12);
                ModConfig.ToggleEnabled(saveImmediate: true);
            };

            mi.Invoke(null, new object[] { "ReduceMouseLag.Toggle", ui_img });
        }
    }
}
