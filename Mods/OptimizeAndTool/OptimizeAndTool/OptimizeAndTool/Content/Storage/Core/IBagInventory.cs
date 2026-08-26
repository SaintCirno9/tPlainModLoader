using System;
using System.Collections.Generic;
using Terraria;

namespace OptimizeAndTool.Content.Storage.Core
{
    /// <summary>
    /// 通用容器数据源契约接口：
    /// 为大背包、药水袋、旗帜盒、随身饰品袋等所有收纳载体提供统一的数据读写、存取规则与仓储动作标准
    /// 作者: SaintCirno9
    /// </summary>
    public interface IBagInventory
    {
        /// <summary>容器展示标题</summary>
        string Title { get; }

        /// <summary>存储物品数组</summary>
        Item[] Slots { get; }

        /// <summary>总容量大小</summary>
        int Capacity { get; }

        /// <summary>是否允许 Alt/Ctrl 收藏锁定物品</summary>
        bool CanFavorite { get; }

        /// <summary>是否在存有多 Mod 物品时展示 Mod 来源侧边栏</summary>
        bool ShowModSidebar { get; }

        /// <summary>
        /// 物品准入限制与查重校验
        /// </summary>
        /// <param name="item">待检验物品</param>
        /// <param name="targetSlot">目标槽位索引（-1 为任意槽）</param>
        /// <returns>若允许放入返回 true</returns>
        bool MeetEntryCriteria(Item item, int targetSlot = -1);

        /// <summary>
        /// 尝试向容器存入物品（自动堆叠或放入空格）
        /// </summary>
        bool TryDeposit(Item item, bool sort = true);

        /// <summary>
        /// 尝试从外部来源槽位（如玩家背包）转移存入
        /// </summary>
        bool TryDepositFromSlot(Item[] inv, int slot, bool justCheck);

        /// <summary>一键全部存入</summary>
        void DepositAll(Player player);

        /// <summary>一键快速堆叠</summary>
        void QuickStack(Player player);

        /// <summary>一键全部取出</summary>
        void LootAll(Player player);

        /// <summary>一键整理排序</summary>
        void Sort();

        /// <summary>获取容量统计文本（如 "已存: 12/40"）</summary>
        string GetCapacityText();

        /// <summary>槽位发生变动事件通知</summary>
        event Action OnSlotsChanged;

        /// <summary>触发槽位变动事件</summary>
        void TriggerSlotsChanged();
    }

    /// <summary>
    /// 槽位外观显隐扩展能力（饰品袋等具备外观可见性开关的容器实现）
    /// </summary>
    public interface IVisualToggleable
    {
        /// <summary>外观隐藏标记数组</summary>
        bool[] HideVisuals { get; }

        /// <summary>切换单格外观显隐</summary>
        void ToggleVisual(int slot);

        /// <summary>一键切换全部外观显隐</summary>
        void ToggleAllVisuals();

        /// <summary>是否存在任意显示外观的槽位</summary>
        bool HasAnyVisibleVisuals();
    }

    /// <summary>
    /// 自定义工具栏动作扩展能力（用于向 UniversalBagWindow 顶部工具栏注入特异性开关按钮）
    /// </summary>
    public interface IToolbarCustomActions
    {
        /// <summary>获取自定义工具栏按钮列表</summary>
        IEnumerable<BagToolbarButton> GetCustomToolbarButtons();
    }
}
