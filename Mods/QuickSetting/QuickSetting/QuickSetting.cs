using Microsoft.Xna.Framework.Graphics;
using QuickSetting.KeyBind;
using QuickSetting.UI;
using System;
using System.Collections.Generic;
using TPML;
using Terraria;
using Terraria.UI;

namespace QuickSetting
{
    public class QuickSetting : Mod
    {
        public static Action<string> OnAddItem = null;
        public static Action<string, string> OnSwitchItem = null;
        public static Action OnWindowGeometryChanged = null;

        private static UIQuickSetting ui_qs = null;
        private static List<string> keyOrder = null;
        private static (float? x, float? y, float? w, float? h) pendingGeometry = (null, null, null, null);

        public override void Load()
        {
            QuickSettingKeybind.Initialize();

            if (Main.dedServ) return;

            ui_qs = new UIQuickSetting("设置", 350, 600);
            if (pendingGeometry.x.HasValue || pendingGeometry.y.HasValue || pendingGeometry.w.HasValue || pendingGeometry.h.HasValue)
            {
                ui_qs.SetGeometry(pendingGeometry.x, pendingGeometry.y, pendingGeometry.w, pendingGeometry.h);
            }

            ui_qs.OnAddItem += (s1) =>
            {
                OnAddItem?.Invoke(s1);
                if (keyOrder != null) ui_qs.KeyOrder(keyOrder);
            };
            ui_qs.OnSwitchItem += (s1, s2) =>
            {
                OnSwitchItem?.Invoke(s1, s2);
                if (keyOrder != null) ui_qs.KeyOrder(keyOrder);
            };
        }

        public static void SwitchOpenOrClose()
        {
            if (Main.dedServ || ui_qs == null) return;

            if (ui_qs.IsOpen) ui_qs.Close();
            else ui_qs.Open(ModifyInterfaceLayers.ui_state);
        }

        public static void AddItem(Texture2D ico, string name, UIElement uie)
        {
            if (Main.dedServ || ui_qs == null) return;
            ui_qs.AddItem(ico, name, uie);
        }

        public static void SetKeyOrder(List<string> keyOrder)
        {
            QuickSetting.keyOrder = keyOrder;
            if (ui_qs != null && keyOrder != null)
            {
                ui_qs.KeyOrder(keyOrder);
            }
        }

        public static void SetWindowGeometry(float? x, float? y, float? w, float? h)
        {
            pendingGeometry = (x, y, w, h);
            if (ui_qs != null)
            {
                ui_qs.SetGeometry(x, y, w, h);
            }
        }

        public static (float? x, float? y, float? w, float? h) GetWindowGeometry()
        {
            if (ui_qs == null) return pendingGeometry;
            return (ui_qs.Left.Pixels, ui_qs.Top.Pixels, ui_qs.Width.Pixels, ui_qs.Height.Pixels);
        }
    }
}
