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

namespace OptimizeAndTool.ModLinkage
{
    /// <summary>
    /// QuickButton 悬浮工具栏接入：巨大背包快捷按钮
    /// 作者: SaintCirno9
    /// </summary>
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

            UIImage ui_img = new UIImage(Main.Assets.Request<Texture2D>("Images/Item_4131", ReLogic.Content.AssetRequestMode.ImmediateLoad));
            ui_img.Width.Pixels = 32;
            ui_img.Height.Pixels = 32;
            ui_img.ScaleToFit = true;
            ui_img.OnUpdate += _ =>
            {
                if (ui_img.IsMouseHovering)
                {
                    string k = Content.BigBag.BigBag.HotKey.val;
                    Main.instance.MouseText(string.IsNullOrEmpty(k) ? "巨大背包" : $"巨大背包 ({k})");
                }
            };
            ui_img.OnLeftClick += (e, s) =>
            {
                SoundEngine.PlaySound(12);
                ModifyInterfaceLayers.SwitchBigBag();
            };

            mi.Invoke(null, new object[] { "OptimizeAndTool.BigBag.Switch", ui_img });
        }
    }
}
