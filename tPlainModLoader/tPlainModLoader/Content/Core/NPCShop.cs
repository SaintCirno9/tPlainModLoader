using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using TPML.Content.Engine;

namespace TPML.Content
{
    /// <summary>
    /// TPML NPC 商店抽象基类
    /// </summary>
    public abstract class AbstractNPCShop
    {
        public int NpcType { get; }
        public string Name { get; }
        public string FullName => $"{NpcType}/{Name}";
        public virtual IEnumerable<NPCShop.Entry> ActiveEntries => Enumerable.Empty<NPCShop.Entry>();

        protected AbstractNPCShop(int npcType, string name = "Shop")
        {
            NpcType = npcType;
            Name = name;
        }

        public abstract void FillShop(Item[] items, NPC npc);
        public abstract void Register();
    }

    /// <summary>
    /// TPML 原生强类型 NPC 商店配置器
    /// </summary>
    public class NPCShop : AbstractNPCShop
    {
        public class Entry
        {
            public Item Item { get; }
            public List<Condition> Conditions { get; } = new List<Condition>();

            public Entry(Item item, IEnumerable<Condition> conditions = null)
            {
                Item = item ?? new Item();
                if (conditions != null)
                {
                    Conditions.AddRange(conditions.Where(c => c != null));
                }
            }

            public bool Disabled => false;
            public bool Visible => Conditions.TrueForAll(c => c == null || c.IsMet());
        }

        private readonly List<Entry> _entries = new List<Entry>();
        public IReadOnlyList<Entry> Entries => _entries;
        public override IEnumerable<Entry> ActiveEntries => _entries;

        public NPCShop(int npcType, string name = "Shop") : base(npcType, name)
        {
        }

        public NPCShop Add(Item item, params Condition[] conditions)
        {
            _entries.Add(new Entry(item, conditions));
            return this;
        }

        public NPCShop Add(int itemType, params Condition[] conditions)
        {
            Item it = new Item();
            it.SetDefaults(itemType);
            return Add(it, conditions);
        }

        public override void FillShop(Item[] items, NPC npc)
        {
            int index = 0;
            foreach (var entry in _entries)
            {
                if (index >= items.Length) break;
                if (entry.Visible && !entry.Disabled)
                {
                    items[index++] = entry.Item.Clone();
                }
            }
        }

        public override void Register()
        {
            NPCShopDatabase.Register(this);
            foreach (var gNpc in ContentHookDispatcher.ActiveGlobalNPCs)
            {
                try
                {
                    gNpc.ModifyShop(this);
                }
                catch (Exception ex)
                {
                    ModLoader.Log($"[NPCShop] GlobalNPC.ModifyShop 异常: {ex.Message}");
                }
            }
        }
    }

    /// <summary>
    /// TPML 模组 NPC 商店数据库
    /// </summary>
    public static class NPCShopDatabase
    {
        private static readonly List<AbstractNPCShop> _shops = new List<AbstractNPCShop>();
        public static IReadOnlyList<AbstractNPCShop> AllShops => _shops;

        public static void Register(AbstractNPCShop shop)
        {
            if (shop != null && !_shops.Contains(shop))
            {
                _shops.Add(shop);
            }
        }

        public static AbstractNPCShop GetShop(int npcType, string shopName)
        {
            return _shops.Find(s => s.NpcType == npcType && string.Equals(s.Name, shopName, StringComparison.OrdinalIgnoreCase));
        }

        public static void Clear()
        {
            _shops.Clear();
        }
    }
}
