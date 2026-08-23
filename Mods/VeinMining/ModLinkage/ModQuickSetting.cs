using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using tContentPatch;
using Terraria;
using Terraria.UI;
using VeinMining.Config;

namespace VeinMining.ModLinkage
{
    /// <summary>
    /// QuickSetting 抽屉菜单联动
    /// </summary>
    public class ModQuickSetting : Mod
    {
        public static bool IsLinkage = true;

        public override void Loaded()
        {
            if (Main.dedServ) return;
            if (!IsLinkage) return;

            List<tContentPatch.ModLoad.ModObject> mos = ContentPatch.GetModObjects();
            if (mos == null) return;

            tContentPatch.ModLoad.ModObject mo = mos.FirstOrDefault(i => i.config.key == "StaticTile.QuickSetting");
            if (mo == null) return;

            Type type = mo.assembly.GetType("QuickSetting.QuickSetting.QuickSetting");
            if (type == null) return;

            MethodInfo mi = type.GetMethod("AddItem", BindingFlags.Static | BindingFlags.Public);
            if (mi == null) return;

            Texture2D texture = Main.Assets.Request<Texture2D>("Images/Item_3507", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            List<UIElement> uis = VeinMiningSetting.Instance?.CreateSettingUIElements();
            if (uis != null)
            {
                foreach (UIElement ui in uis)
                {
                    mi.Invoke(null, new object[] { texture, "连锁挖矿", ui });
                }
            }
        }
    }
}
