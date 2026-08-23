using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using tContentPatch;
using Terraria;
using Terraria.UI;

namespace ReduceMouseLag.ModLinkage
{
    public class ModQuickSetting : Mod
    {
        public static bool IsLinkage = true;

        public override void Loaded()
        {
            if (Main.dedServ) return;
            if (IsLinkage == false) return;

            List<tContentPatch.ModLoad.ModObject> mos = ContentPatch.GetModObjects();
            if (mos == null) return;

            tContentPatch.ModLoad.ModObject mo = mos.FirstOrDefault(i => i.config.key == "StaticTile.QuickSetting");
            if (mo == null) return;

            Type type = mo.assembly.GetType("QuickSetting.QuickSetting.QuickSetting");
            if (type == null) return;

            System.Reflection.MethodInfo mi = type.GetMethod("AddItem", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            if (mi == null) return;

            Texture2D texture = Main.Assets.Request<Texture2D>("Images/UI/Cursor_0", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            foreach (UIElement ui in ModConfig.GetUIElements())
            {
                mi.Invoke(null, new object[] { texture, "鼠标延迟优化", ui });
            }
        }
    }
}
