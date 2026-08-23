using System.Collections.Generic;
using Terraria.ID;
using VeinMining.Config;

namespace VeinMining.Core
{
    /// <summary>
    /// 连锁挖矿物块分类集合与快速索引表
    /// </summary>
    public static class VeinMiningSets
    {
        /// <summary>
        /// 矿石类图格查找表 (O(1) 检索)
        /// </summary>
        public static readonly bool[] IsOre = new bool[65536];

        /// <summary>
        /// 地下宝石与沙漠化石图格查找表
        /// </summary>
        public static readonly bool[] IsGem = new bool[65536];

        /// <summary>
        /// 泥土/石头/沙块等普通杂块查找表
        /// </summary>
        public static readonly bool[] IsTrash = new bool[65536];

        static VeinMiningSets()
        {
            // 原版矿石 ID 列表 (包含肉前所有矿石、肉后所有新三矿/精金钛金、叶绿矿、夜明矿、陨石、狱石、黑曜石等)
            ushort[] ores = new ushort[]
            {
                TileID.Copper,      // 7 铜矿
                TileID.Tin,         // 166 锡矿
                TileID.Iron,        // 6 铁矿
                TileID.Lead,        // 167 铅矿
                TileID.Silver,      // 9 银矿
                TileID.Tungsten,    // 168 钨矿
                TileID.Gold,        // 8 金矿
                TileID.Platinum,    // 169 铂金矿
                TileID.Demonite,    // 22 魔矿
                TileID.Crimtane,    // 204 猩红矿
                TileID.Meteorite,   // 37 陨石
                TileID.Obsidian,    // 56 黑曜石
                TileID.Hellstone,   // 58 狱石
                TileID.Cobalt,      // 107 钴矿
                TileID.Palladium,   // 221 钯金矿
                TileID.Mythril,     // 108 秘银矿
                TileID.Orichalcum,  // 222 山铜矿
                TileID.Adamantite,  // 111 精金矿
                TileID.Titanium,    // 223 钛金矿
                TileID.Chlorophyte, // 211 叶绿矿
                TileID.LunarOre,    // 408 夜明矿
                TileID.DesertFossil // 404 沙漠化石
            };

            foreach (ushort id in ores)
            {
                IsOre[id] = true;
            }

            // 宝石类 ID 列表 (紫晶、黄玉、蓝玉、翡翠、红玉、钻石、嵌石宝石)
            ushort[] gems = new ushort[]
            {
                TileID.Amethyst,        // 63 紫晶
                TileID.Topaz,           // 64 黄玉
                TileID.Sapphire,        // 65 蓝玉
                TileID.Emerald,         // 66 翡翠
                TileID.Ruby,            // 67 红玉
                TileID.Diamond,         // 68 钻石
                TileID.ExposedGems      // 178 嵌石宝石块
            };

            foreach (ushort id in gems)
            {
                IsGem[id] = true;
            }

            // 泥土/石头/沙等常见地表与地下杂块列表
            ushort[] trash = new ushort[]
            {
                TileID.Dirt,                 // 0 泥土
                TileID.Stone,                // 1 石头
                TileID.Mud,                  // 59 泥块
                TileID.Sand,                 // 53 沙块
                TileID.Ebonsand,             // 112 黑檀沙
                TileID.Crimsand,             // 234 猩红沙
                TileID.Pearlsand,            // 116 珍珠沙
                TileID.SnowBlock,            // 147 雪块
                TileID.IceBlock,             // 161 冰雪块
                TileID.CorruptIce,           // 163 紫冰雪块
                TileID.HallowedIce,          // 164 粉冰雪块
                TileID.FleshIce,             // 200 红冰雪块
                TileID.Granite,              // 368 花岗岩
                TileID.Marble,               // 367 大理石
                TileID.ClayBlock,            // 40 粘土块
                TileID.Ash,                  // 57 灰烬块
                TileID.HardenedSand,         // 397 硬化沙
                TileID.CorruptHardenedSand,  // 398
                TileID.CrimsonHardenedSand,  // 399
                TileID.HallowHardenedSand,   // 402
                TileID.Sandstone,            // 396 砂岩
                TileID.CorruptSandstone,     // 400
                TileID.CrimsonSandstone,     // 401
                TileID.HallowSandstone,      // 403
                TileID.Ebonstone,            // 25 黑檀石
                TileID.Crimstone,            // 203 猩红石
                TileID.Pearlstone,           // 117 珍珠石
                TileID.Silt,                 // 123 泥沙
                TileID.Slush                 // 224 雪泥
            };

            foreach (ushort id in trash)
            {
                IsTrash[id] = true;
            }
        }

        /// <summary>
        /// 判定指定物块类型是否符合当前配置下的连锁挖掘条件
        /// </summary>
        /// <param name="type">图格类型 ID</param>
        /// <returns>是否可连锁挖掘</returns>
        public static bool IsMinable(ushort type)
        {
            if (IsOre[type]) return true;
            if (VeinMiningConfig.MineGems && IsGem[type]) return true;
            if (VeinMiningConfig.MineTrashTiles && IsTrash[type]) return true;
            return false;
        }
    }
}
