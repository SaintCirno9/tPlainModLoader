using System;
using Terraria;

namespace AccessoryBox.Common
{
    internal class BoxConsole
    {
        public readonly Func<Item, bool> AddItem = null;
        public readonly Action<Item> DelItem = null;
        public readonly Action ClearItem = null;
        public readonly Action<bool> EnableSet = null;
        public readonly Func<bool> EnableGet = null;
        public readonly Action Load = null;
        public readonly Action Save = null;

        public BoxConsole(Func<Item, bool> addItem, Action<Item> delItem, Action clearItem, Action<bool> enableSet, Func<bool> enableGet, Action load, Action save)
        {
            AddItem = addItem;
            DelItem = delItem;
            ClearItem = clearItem;
            EnableSet = enableSet;
            EnableGet = enableGet;
            Load = load;
            Save = save;
        }
    }
}
