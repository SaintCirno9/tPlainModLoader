using System;
using Terraria;
using TPML.Content.IO;

namespace OptimizeAndTool.Content.Storage.AccessoryBox
{
    /// <summary>
    /// 随身饰品箱角色伴随存档持久化 (Sidecar Containers: "AccessoryBox")
    /// 完全绑定角色生命周期，杜绝跨人物共享，支持原版与模组实体物品无损存读档
    /// 作者: SaintCirno9
    /// </summary>
    public static class AccessoryBoxStorage
    {
        public const string ContainerKey = "AccessoryBox";

        /// <summary>
        /// 立即将当前活动玩家的随身饰品箱数据保存落盘至 Sidecar 伴随文件
        /// </summary>
        public static void SaveNow()
        {
            Player player = Main.LocalPlayer;
            if (player == null) return;
            ModItemSidecarEngine.SavePlayerContainer(player, ContainerKey, AccessoryBox.Slots);
        }

        /// <summary>
        /// 为指定玩家加载其专属的随身饰品箱数据
        /// </summary>
        public static void LoadForPlayer(Player player)
        {
            if (player == null) return;
            int cap = AccessoryBox.Capacity.val;
            Item[] slots = ModItemSidecarEngine.LoadPlayerContainer(player, ContainerKey, cap);
            AccessoryBox.SetItems(slots);
        }
    }
}
