using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.GameContent.Personalities;
using Terraria.ID;

namespace TPML.Content
{
    /// <summary>
    /// 对齐 tML IShoppingBiome 接口
    /// </summary>
    public interface IShoppingBiome
    {
        string NameKey { get; }
        bool IsInBiome(Player player);
    }

    /// <summary>
    /// 对齐 tML NPCHappiness 结构体
    /// 作者: SaintCirno9
    /// </summary>
    public readonly struct NPCHappiness
    {
        public readonly int NpcType;

        private NPCHappiness(int npcType)
        {
            NpcType = npcType;
        }

        public NPCHappiness SetNPCAffection<T>(AffectionLevel affectionLevel) where T : ModNPC
        {
            return SetNPCAffection(ModContent.NPCType<T>(), affectionLevel);
        }

        public NPCHappiness SetNPCAffection(int npcId, AffectionLevel affectionLevel)
        {
            try
            {
                var database = Main.ShopHelper?._database;
                if (database == null) return this;
                var profile = database.GetByNPCID(NpcType);
                if (profile == null) return this;
                var shopModifiers = profile.ShopModifiers;
                if (shopModifiers == null) return this;

                NPCPreferenceTrait trait = (NPCPreferenceTrait)shopModifiers.FirstOrDefault(t => t is NPCPreferenceTrait n && n.NpcId == npcId);
                if (trait != null)
                {
                    trait.Level = affectionLevel;
                    return this;
                }
                shopModifiers.Add(new NPCPreferenceTrait
                {
                    NpcId = npcId,
                    Level = affectionLevel
                });
            }
            catch { }
            return this;
        }

        public NPCHappiness SetBiomeAffection<T>(AffectionLevel affectionLevel) where T : AShoppingBiome, new()
        {
            try
            {
                var database = Main.ShopHelper?._database;
                if (database == null) return this;
                var profile = database.GetByNPCID(NpcType);
                if (profile == null) return this;
                var shopModifiers = profile.ShopModifiers;
                if (shopModifiers == null) return this;

                BiomePreferenceListTrait listTrait = (BiomePreferenceListTrait)shopModifiers.FirstOrDefault(t => t is BiomePreferenceListTrait);
                if (listTrait == null)
                {
                    listTrait = new BiomePreferenceListTrait();
                    shopModifiers.Add(listTrait);
                }
                listTrait.Add(new BiomePreferenceListTrait.BiomePreference(affectionLevel, new T()));
            }
            catch { }
            return this;
        }

        public NPCHappiness SetBiomeAffection(AShoppingBiome biome, AffectionLevel affectionLevel)
        {
            try
            {
                var database = Main.ShopHelper?._database;
                if (database == null) return this;
                var profile = database.GetByNPCID(NpcType);
                if (profile == null) return this;
                var shopModifiers = profile.ShopModifiers;
                if (shopModifiers == null) return this;

                BiomePreferenceListTrait listTrait = (BiomePreferenceListTrait)shopModifiers.FirstOrDefault(t => t is BiomePreferenceListTrait);
                if (listTrait == null)
                {
                    listTrait = new BiomePreferenceListTrait();
                    shopModifiers.Add(listTrait);
                }
                if (biome != null)
                {
                    listTrait.Add(new BiomePreferenceListTrait.BiomePreference(affectionLevel, biome));
                }
            }
            catch { }
            return this;
        }

        public static NPCHappiness Get(int npcType)
        {
            return new NPCHappiness(npcType);
        }
    }
}
