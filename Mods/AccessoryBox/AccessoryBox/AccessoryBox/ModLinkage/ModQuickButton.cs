using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using tContentPatch;
using tContentPatch.ModLoad;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;

namespace AccessoryBox.ModLinkage
{
    public class ModQuickButton : Mod
    {
        public override void Loaded()
        {
            if (Main.dedServ) return;

            List<ModObject> mos = ContentPatch.GetModObjects();
            if (mos == null) return;

            ModObject mo = mos.FirstOrDefault(i => i.assembly?.GetName().Name == "QuickButton");
            if (mo == null) return;

            Type type = mo.assembly.GetType("QuickButton.QuickButton.QuickButton");
            if (type == null) return;

            MethodInfo mi = type.GetMethod("Add", BindingFlags.Static | BindingFlags.Public);
            if (mi == null) return;

            UIImage ui_img = new UIImage(Main.Assets.Request<Texture2D>("Images/Item_1862", ReLogic.Content.AssetRequestMode.ImmediateLoad));
            ui_img.Width.Pixels = 32;
            ui_img.Height.Pixels = 32;
            ui_img.ScaleToFit = true;
            ui_img.OnUpdate += _ =>
            {
                if (ui_img.IsMouseHovering) Main.instance.MouseText("饰品箱");
            };
            ui_img.OnLeftClick += (e, s) =>
            {
                SoundEngine.PlaySound(SoundID.MenuTick);
                ModifyInterfaceLayers.SwitchBox();
            };

            mi.Invoke(null, new object[] { "AccessoryBox.SwitchOpenOrClose", ui_img });
        }
    }
}
