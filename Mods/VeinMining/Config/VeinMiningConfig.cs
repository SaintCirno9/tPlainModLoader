using System;

namespace VeinMining.Config
{
    /// <summary>
    /// 连锁挖矿 JSON 持久化数据结构
    /// </summary>
    public class VeinMiningData
    {
        public bool enable = true;
        public int maxTiles = 200;
        public bool mineTrashTiles = false;
        public bool mineGems = true;
    }

    /// <summary>
    /// 连锁挖矿全局配置内存状态
    /// </summary>
    public static class VeinMiningConfig
    {
        /// <summary>
        /// 是否启用连锁挖矿
        /// </summary>
        public static bool Enable { get; set; } = true;

        /// <summary>
        /// 最大连锁破坏数量上限 (默认 200)
        /// </summary>
        public static int MaxTiles { get; set; } = 200;

        /// <summary>
        /// 是否连锁破坏泥土/石头等普通杂块
        /// </summary>
        public static bool MineTrashTiles { get; set; } = false;

        /// <summary>
        /// 是否连锁破坏地下宝石与沙漠化石
        /// </summary>
        public static bool MineGems { get; set; } = true;

        /// <summary>
        /// 配置变更通知事件
        /// </summary>
        public static Action OnConfigChanged;
    }
}
