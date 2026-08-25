using System;
using Terraria;

namespace TPML.Content.Fusion
{
    /// <summary>
    /// 通用外部背包/容器数据源提供者接口。<br/>
    /// 任何模组容器（大背包 BigBag、药水袋 PotionBag、旗帜盒 BannerChest、随身便携箱、虚拟仓库等）<br/>
    /// 均可通过实现此接口并向 <see cref="InventoryFusionManager"/> 注册，<br/>
    /// 获得原版全套背包查询（HasItem、CountItem）、自动扣除（ConsumeItem）、魔杖放置（tileWand/FlexibleTileWand）、油漆识别等无缝融合支持。
    /// 作者: SaintCirno9
    /// </summary>
    public interface IFusionItemSource
    {
        /// <summary>
        /// 数据源唯一标识符（例如 "OptimizeAndTool.BigBag"）
        /// </summary>
        string Id { get; }

        /// <summary>
        /// 优先级权重（数值越小优先级越高，决定多容器并存时的检索与扣减次序）
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// 判断当前数据源对指定玩家是否处于激活/可用状态
        /// </summary>
        /// <param name="player">目标玩家实体</param>
        /// <returns>若处于激活状态则返回 true</returns>
        bool IsActive(Player player);

        /// <summary>
        /// 获取该数据源提供的物品槽位数组
        /// </summary>
        /// <param name="player">目标玩家实体</param>
        /// <returns>物品槽位数组（若不可用可返回 null）</returns>
        Item[] GetSlots(Player player);

        /// <summary>
        /// 当该数据源中的物品发生变动/消耗时的回调（用于触发写盘持久化与 UI 刷新）
        /// </summary>
        /// <param name="player">目标玩家实体</param>
        void OnModified(Player player);
    }
}
