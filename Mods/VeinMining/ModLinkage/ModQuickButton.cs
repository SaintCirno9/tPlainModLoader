using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using tContentPatch;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using VeinMining.Config;

namespace VeinMining.ModLinkage
{
    /// <summary>
    /// QuickButton 快捷悬浮工具栏联动
    /// </summary>
    public class ModQuickButton : Mod
    {
        public static bool IsLinkage = true;

        public override void Loaded()
        {
            if (Main.dedServ) return;
            if (!IsLinkage) return;

            List<tContentPatch.ModLoad.ModObject> mos = ContentPatch.GetModObjects();
            if (mos == null) return;

            tContentPatch.ModLoad.ModObject mo = mos.FirstOrDefault(i => i.assembly?.GetName().Name == "QuickButton");
            if (mo == null) return;

            Type type = mo.assembly.GetType("QuickButton.QuickButton.QuickButton");
            if (type == null) return;

            MethodInfo mi = type.GetMethod("Add", BindingFlags.Static | BindingFlags.Public);
            if (mi == null) return;

            Texture2D ico = Main.Assets.Request<Texture2D>("Images/Item_3507", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            UIImage ui_img = new UIImage(ico);
            ui_img.Width.Pixels = 32;
            ui_img.Height.Pixels = 32;
            ui_img.ScaleToFit = true;
            ui_img.OnUpdate += _ =>
            {
                if (ui_img.IsMouseHovering)
                {
                    Main.instance.MouseText($"简单连锁挖矿: {(VeinMiningConfig.Enable ? "已开启" : "已关闭")}");
                }
            };
            ui_img.OnLeftClick += (e, s) =>
            {
                SoundEngine.PlaySound(12);
                VeinMiningConfig.Enable = !VeinMiningConfig.Enable;
                VeinMiningSetting.Instance?.Save();
                VeinMiningConfig.OnConfigChanged?.Invoke();
            };

            mi.Invoke(null, new object[] { "VeinMining.SwitchEnable", ui_img });
        }
    }
}
