using System;
using System.Collections.Generic;
using System.Reflection;
using tContentPatch;
using Terraria;
using Terraria.ID;

namespace AccessoryBox.Common
{
    internal class AccessoryBox : PatchPlayer
    {
        public bool Enable
        {
            get => Config.Instance?.GetVal() ?? false;
            set => Config.Instance?.SetVal(value);
        }
        protected static List<Item> armor = new List<Item>();

        public override void Initialize()
        {
            ModifyInterfaceLayers.SetConsole(new BoxConsole(
                AddItem,
                item => armor.Remove(item),
                () => armor.Clear(),
                v => Enable = v,
                () => Enable,
                () => ItemListConfig.LoadData(),
                () => ItemListConfig.SaveData(armor)));
        }

        public static void LoadItems(List<Item> items)
        {
            armor = new List<Item>();

            if (items == null) return;
            
            foreach (Item i in items)
            {
                if (AddItem(i) == false) return;
            }
        }

        private static bool AddItem(Item item)
        {
            if (armor.Count > 1145) return false;

            armor.Add(item.Clone());
            return true;
        }

        public override void UpdateEquipsPostfix(Player This, int playerI)
        {
            if (This != Main.LocalPlayer || Main.dedServ) return;
            if (Enable == false) return;

            List<Item> armor = AccessoryBox.armor;

            //盔甲饰品
            ForA((item, k) =>
            {
                if (item.accessory)//饰品
                {
                    F(This, "GrantPrefixBenefits", item);
                }
                F(This, "GrantArmorBenefits", item);
            });

            //饰品
            ForA((item, m) =>
            {
                F(This, "ApplyEquipFunctional", 3, armor[m]);
            });

            //饰品
            ForA((item, n) =>
            {
                if (armor[n].wingSlot > 0)
                {
                    if (!This.hideVisibleAccessory[3] || (This.velocity.Y != 0f && This.mount.CanUseWings))
                    {
                        This.wings = armor[n].wingSlot;
                    }
                    This.wingsLogic = armor[n].wingSlot;
                }
            });

            //饰品时装栏
            ForA((item, num) =>
            {
                F(This, "ApplyEquipVanity", 13, armor[num]);
            });
        }

        private static void ForA(Action<Item, int> a)
        {
            for (int i = 0; i < armor.Count; ++i)
            {
                if (armor[i].type <= ItemID.None) continue;
                a(armor[i], i);
            }
        }

        private static object F(Player This, string name, params object[] args)
        {
            MethodInfo m = typeof(Player).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic);
            return m.Invoke(This, args);
        }
    }
}
