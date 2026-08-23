using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using tContentPatch;
using Terraria;
using Terraria.UI;

namespace QuickButton.QuickButton
{
    public class QuickButton : Mod
    {
        private static UIQuickButton ui_qb = null;
        private static List<string> keys = null;

        /// <summary>
        /// 按钮被右键拖动结束后回调最终位置
        /// </summary>
        public static Action<Vector2> OnMoved = null;

        public override void Load()
        {
            if (Main.dedServ) return;

            ui_qb = new UIQuickButton();
            ui_qb.OnMoved += pos => OnMoved?.Invoke(pos);
            ModifyInterfaceLayers.ui_state.Append(ui_qb);

            keys = new List<string>();
        }

        public static void Initialize_Key(List<string> keyOrder)
        {
            if (Main.dedServ) return;
            if (keyOrder == null) return;

            keys.Clear();

            foreach (string i in keyOrder)
            {
                if (i == null) continue;
                if (keys.Contains(i)) continue;

                keys.Add(i);
            }

            ui_qb.KeyOrder(keys);
        }

        public static void Add(string key, UIElement uie)
        {
            if (key == null || uie == null) return;

            if (keys.Contains(key) == false) keys.Add(key);

            ui_qb.Add(key, uie);
            ui_qb.KeyOrder(keys);
        }

        public static void SetPos(int pos)
        {
            if (Main.dedServ) return;
            ui_qb.SetPos(pos);
        }

        /// <summary>
        /// 应用持久化的自定义拖动位置
        /// </summary>
        public static void SetMovePos(float x, float y)
        {
            if (Main.dedServ) return;
            ui_qb.SetPos(x, y);
        }

        public static List<string> GetContainsKey() => ui_qb.GetKeys();

        public static List<string> GetAllKey() => keys.ToList();

        public static void Exchange(string key1, string key2)
        {
            if (key1 == null || key2 == null) return;
            if (key1 == key2) return;

            int index1 = keys.IndexOf(key1);
            int index2 = keys.IndexOf(key2);
            if (index1 == -1 || index2 == -1) return;

            keys[index1] = key2;
            keys[index2] = key1;

            ui_qb.KeyOrder(keys);
        }
    }
}
