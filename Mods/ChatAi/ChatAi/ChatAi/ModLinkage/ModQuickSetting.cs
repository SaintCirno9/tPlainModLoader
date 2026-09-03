using ChatAi.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using TPML;
using Terraria;
using Terraria.UI;

namespace ChatAi.ModLinkage
{
    public class ModQuickSetting : Mod
    {
        public static bool IsLinkage = true;

        public override void Loaded()
        {
            if (Main.dedServ) return;

            List<TPML.ModLoad.ModObject> mos = ContentPatch.GetModObjects();
            if (mos == null) return;

            TPML.ModLoad.ModObject mo = mos.FirstOrDefault(i => i.config.key == "StaticTile.QuickSetting");
            if (mo == null) return;

            Type type = mo.assembly.GetType("QuickSetting.QuickSetting");
            if (type == null) return;

            System.Reflection.MethodInfo mi = type.GetMethod("AddItem", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            if (mi == null) return;

            if (IsLinkage)
            {
                AddItem(mi, "Images/NPC_Head_8", "聊天Ai", GameChatAi.GetUI());
            }
        }

        private static void AddItem(System.Reflection.MethodInfo mi, string ico, string text, List<UIElement> uis)
        {
            Texture2D texture = Main.Assets.Request<Texture2D>(ico, ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;

            foreach (UIElement ui in uis) mi.Invoke(null, new object[] { texture, text, ui });
        }
    }
}
