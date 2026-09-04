using System.Collections.Generic;
using Terraria.ID;

namespace OptimizeAndTool.Content.QoL.VeinMining
{
    /// <summary>
    /// 连锁挖矿物块分类集合与快速索引表
    /// 作者: SaintCirno9
    /// </summary>
    public static class VeinMiningSets
    {
        public static readonly bool[] IsOre = new bool[65536];
        public static readonly bool[] IsGem = new bool[65536];
        public static readonly bool[] IsTrash = new bool[65536];

        static VeinMiningSets()
        {
            ushort[] ores = new ushort[]
            {
                TileID.Copper, TileID.Tin, TileID.Iron, TileID.Lead,
                TileID.Silver, TileID.Tungsten, TileID.Gold, TileID.Platinum,
                TileID.Demonite, TileID.Crimtane, TileID.Meteorite, TileID.Obsidian,
                TileID.Hellstone, TileID.Cobalt, TileID.Palladium, TileID.Mythril,
                TileID.Orichalcum, TileID.Adamantite, TileID.Titanium, TileID.Chlorophyte,
                TileID.LunarOre, TileID.DesertFossil
            };

            foreach (ushort id in ores)
            {
                IsOre[id] = true;
            }

            ushort[] gems = new ushort[]
            {
                TileID.Amethyst, TileID.Topaz, TileID.Sapphire,
                TileID.Emerald, TileID.Ruby, TileID.Diamond,
                TileID.ExposedGems
            };

            foreach (ushort id in gems)
            {
                IsGem[id] = true;
            }

            ushort[] trash = new ushort[]
            {
                TileID.Dirt, TileID.Stone, TileID.Mud, TileID.Sand,
                TileID.Ebonsand, TileID.Crimsand, TileID.Pearlsand,
                TileID.SnowBlock, TileID.IceBlock, TileID.CorruptIce,
                TileID.HallowedIce, TileID.FleshIce, TileID.Granite,
                TileID.Marble, TileID.ClayBlock, TileID.Ash,
                TileID.HardenedSand, TileID.CorruptHardenedSand,
                TileID.CrimsonHardenedSand, TileID.HallowHardenedSand,
                TileID.Sandstone, TileID.CorruptSandstone,
                TileID.CrimsonSandstone, TileID.HallowSandstone
            };

            foreach (ushort id in trash)
            {
                IsTrash[id] = true;
            }
        }

        public static bool ShouldMine(ushort tileType, bool includeOres, bool includeGems, bool includeTrash)
        {
            if (includeOres && IsOre[tileType]) return true;
            if (includeGems && IsGem[tileType]) return true;
            if (includeTrash && IsTrash[tileType]) return true;
            return false;
        }
    }
}
