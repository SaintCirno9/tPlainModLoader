using System;
using System.Collections.Generic;
using System.Linq;

namespace OptimizeAndTool.Content.QoL.InfiniteBuff
{
    /// <summary>
    /// 无限增益用户偏好存储管理器：
    /// 统一收拢接入 OptimizeAndTool 的全局设置 (setting.json)，杜绝外部散落独立文件。
    /// 作者: SaintCirno9
    /// </summary>
    public static class InfiniteBuffStorage
    {
        private static readonly HashSet<int> blacklist = new HashSet<int>();
        private static readonly HashSet<int> favorites = new HashSet<int>();

        /// <summary>黑名单 Buff ID 集合（处于黑名单的增益不会生效且被强制清除）</summary>
        public static HashSet<int> Blacklist => blacklist;

        /// <summary>收藏置顶 Buff ID 集合</summary>
        public static HashSet<int> Favorites => favorites;

        /// <summary>增益状态/配置变动事件通知</summary>
        public static event Action OnDataChanged;

        /// <summary>
        /// 从全局设置数据模型载入黑名单与收藏
        /// </summary>
        public static void LoadFromConfig(List<int> configBlacklist, List<int> configFavorites)
        {
            blacklist.Clear();
            if (configBlacklist != null)
            {
                foreach (int id in configBlacklist)
                {
                    blacklist.Add(id);
                }
            }

            favorites.Clear();
            if (configFavorites != null)
            {
                foreach (int id in configFavorites)
                {
                    favorites.Add(id);
                }
            }

            OnDataChanged?.Invoke();
        }

        /// <summary>
        /// 导出当前黑名单列表用于全局设置序列化
        /// </summary>
        public static List<int> ExportBlacklist()
        {
            return blacklist.ToList();
        }

        /// <summary>
        /// 导出当前收藏列表用于全局设置序列化
        /// </summary>
        public static List<int> ExportFavorites()
        {
            return favorites.ToList();
        }

        /// <summary>
        /// 切换指定 Buff 的黑名单状态并触发全局设置持久化
        /// </summary>
        public static void ToggleBlacklist(int buffType)
        {
            if (blacklist.Contains(buffType))
            {
                blacklist.Remove(buffType);
            }
            else
            {
                blacklist.Add(buffType);
            }

            SettingUI_player.SaveSetting();
            OnDataChanged?.Invoke();
        }

        /// <summary>
        /// 切换指定 Buff 的置顶收藏状态并触发全局设置持久化
        /// </summary>
        public static void ToggleFavorite(int buffType)
        {
            if (favorites.Contains(buffType))
            {
                favorites.Remove(buffType);
            }
            else
            {
                favorites.Add(buffType);
            }

            SettingUI_player.SaveSetting();
            OnDataChanged?.Invoke();
        }

        /// <summary>
        /// 一键清空黑名单（全开所有可用 Buff）
        /// </summary>
        public static void ClearBlacklist()
        {
            blacklist.Clear();
            SettingUI_player.SaveSetting();
            OnDataChanged?.Invoke();
        }

        /// <summary>
        /// 一键将指定 Buff 列表全部加入黑名单（全关）
        /// </summary>
        public static void AddAllToBlacklist(IEnumerable<int> buffTypes)
        {
            if (buffTypes != null)
            {
                foreach (int b in buffTypes)
                {
                    blacklist.Add(b);
                }
            }

            SettingUI_player.SaveSetting();
            OnDataChanged?.Invoke();
        }
    }
}
