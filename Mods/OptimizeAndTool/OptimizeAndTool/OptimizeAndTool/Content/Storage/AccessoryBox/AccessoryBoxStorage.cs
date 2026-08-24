using System;
using System.Collections.Generic;
using tContentPatch;
using Terraria;

namespace OptimizeAndTool.Content.Storage.AccessoryBox
{
    /// <summary>
    /// 饰品箱物品持久化（accessoryBox.json），支持 type, prefix, stack 存储，并向下兼容旧路径
    /// 作者: SaintCirno9
    /// </summary>
    public class AccessoryBoxStorage : ModSetting
    {
        public class SlotData
        {
            public int type;
            public int prefix;
            public int stack;
        }

        public override bool HasUI => false;
        public override string FilePath => "accessoryBox.json";
        public override Type DataType => typeof(List<SlotData>);

        private static AccessoryBoxStorage instance = null;
        private List<SlotData> data = null;

        public override void Load(object v)
        {
            instance = this;
            data = v as List<SlotData>;

            if (data == null)
            {
                data = new List<SlotData>();
                NeedSave = true;
                Save();
            }

            int n = Math.Max(AccessoryBox.Capacity.val, Math.Min(500, data.Count));
            Item[] slots = new Item[n];
            for (int i = 0; i < n; i++) slots[i] = new Item();

            for (int i = 0; i < data.Count && i < n; i++)
            {
                SlotData d = data[i];
                if (d == null || d.type <= 0) continue;

                Item item = new Item();
                item.SetDefaults(d.type);
                if (d.prefix > 0) item.Prefix(d.prefix);
                item.stack = d.stack > 1 ? Math.Min(d.stack, item.maxStack) : 1;
                slots[i] = item;
            }

            AccessoryBox.SetItems(slots);
        }

        public override object GetSaveData() => data;

        public static void SaveNow()
        {
            if (instance == null) return;

            List<SlotData> list = new List<SlotData>(AccessoryBox.Slots.Length);
            foreach (Item item in AccessoryBox.Slots)
            {
                SlotData d = new SlotData();
                if (item != null && !item.IsAir && item.type > 0)
                {
                    d.type = item.type;
                    d.prefix = item.prefix;
                    d.stack = item.stack;
                }
                list.Add(d);
            }

            instance.data = list;
            instance.NeedSave = true;
            instance.Save();
        }
    }
}
