using System;
using System.Collections.Generic;
using Terraria;

namespace AccessoryBox.Common
{
    internal interface IBoxConsole
    {
        void AddItem(Item item);
        void DelItem(Item item);
        void ClearItem();
        void SetEnable(bool val);
        bool GetEnable();
        void Load();
        void Save();
        List<Item> GetItems();
        void SetItem(Item item, Item val);
        event Action OnLoaded;
        event Action<Item> OnAdded;
        event Action<Item> OnDeled;
        event Action<Item, Item> OnSetItemed;
        event Action OnClearItemed;
    }
}
