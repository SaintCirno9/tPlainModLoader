using System;
using System.Collections.Generic;
using tContentPatch;
using Terraria;

namespace AccessoryBox
{
    internal class ItemListConfig : ModSetting
    {
        private class ItemData
        {
            public int type;
            public int prefix;

            public ItemData() { }
            public ItemData(int type, int prefix) : this()
            {
                this.type = type;
                this.prefix = prefix;
            }
        }

        public override bool HasUI => false;
        public override string FilePath => "items.json";
        public override Type DataType => typeof(List<ItemData>);
        private static ItemListConfig instance = null;
        private List<ItemData> date = null;

        public override void Load(object v)
        {
            instance = this;

            date = (List<ItemData>)v;

            if (date == null)
            {
                date = new List<ItemData>();
                date.Add(new ItemData());
                date.Add(new ItemData());

                NeedSave = true;
                Save();
            }

            Common.AccessoryBox.LoadItems(date.ConvertAll(i =>
            {
                Item item = new Item();
                item.SetDefaults(i.type);
                item.Prefix(i.prefix);
                return item;
            }));
        }

        public override object GetSaveData() => date;

        public static void LoadData()
        {
            ItemListConfig instance = ItemListConfig.instance;
            if (instance == null) return;

            instance.NeedSave = false;
            instance.Load(instance.Read());
        }

        public static void SaveData(List<Item> items)
        {
            ItemListConfig instance = ItemListConfig.instance;
            if (instance == null) return;

            instance.date = items.ConvertAll(i => new ItemData(i.type, i.prefix));
            instance.NeedSave = true;
            instance.Save();
        }
    }
}
