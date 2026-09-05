using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;

namespace OptimizeAndTool.Content.BigBag
{
    /// <summary>
    /// 词条保护白名单条目元数据
    /// </summary>
    public class PrefixEntry
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public bool IsTopTierDefault { get; set; }
    }

    /// <summary>
    /// 巨大背包词条保护白名单管理器：
    /// 统一驱动全局词条保护策略（白名单中的前缀装备受保护不被出售），接入 setting.json 全局持久化。
    /// 作者: SaintCirno9
    /// </summary>
    public static class PrefixWhitelistManager
    {
        /// <summary>默认受保护的五大核心极品词条 ID 集合</summary>
        public static readonly int[] DefaultTopTierPrefixes = new int[]
        {
            PrefixID.Legendary,  // 81 传奇 (近战)
            PrefixID.Legendary2, // 84 传奇 (巨剑/特殊)
            PrefixID.Unreal,     // 82 虚幻 (远程)
            PrefixID.Mythical,   // 83 神话 (魔法)
            PrefixID.Warding,    // 65 护佑 (饰品防御)
            PrefixID.Menacing    // 72 险恶 (饰品伤害)
        };

        private static readonly HashSet<int> _whitelist = new HashSet<int>(DefaultTopTierPrefixes);

        /// <summary>数据变更事件通知（用于 UI 刷新）</summary>
        public static event Action OnWhitelistChanged;

        /// <summary>
        /// 判定指定前缀 ID 是否处于保护白名单中
        /// </summary>
        public static bool IsWhitelisted(int prefixId)
        {
            if (prefixId <= 0) return false;
            return _whitelist.Contains(prefixId);
        }

        /// <summary>
        /// 切换指定前缀 ID 的白名单状态并即时保存全局设置
        /// </summary>
        public static void TogglePrefix(int prefixId)
        {
            if (prefixId <= 0) return;

            if (_whitelist.Contains(prefixId))
            {
                _whitelist.Remove(prefixId);
            }
            else
            {
                _whitelist.Add(prefixId);
            }

            SettingUI_player.SaveSetting();
            OnWhitelistChanged?.Invoke();
        }

        /// <summary>
        /// 重置为系统预设的默认五大极品词条白名单
        /// </summary>
        public static void ResetToDefault()
        {
            _whitelist.Clear();
            foreach (int id in DefaultTopTierPrefixes)
            {
                _whitelist.Add(id);
            }

            SettingUI_player.SaveSetting();
            OnWhitelistChanged?.Invoke();
        }

        /// <summary>
        /// 全选所有可用前缀
        /// </summary>
        public static void SelectAll(IEnumerable<int> allIds)
        {
            if (allIds == null) return;
            foreach (int id in allIds)
            {
                if (id > 0) _whitelist.Add(id);
            }

            SettingUI_player.SaveSetting();
            OnWhitelistChanged?.Invoke();
        }

        /// <summary>
        /// 清空所有白名单（不保护任何词条）
        /// </summary>
        public static void ClearAll()
        {
            _whitelist.Clear();
            SettingUI_player.SaveSetting();
            OnWhitelistChanged?.Invoke();
        }

        /// <summary>
        /// 从全局配置中恢复白名单
        /// </summary>
        public static void LoadFromConfig(List<int> configList)
        {
            _whitelist.Clear();
            if (configList == null || configList.Count == 0)
            {
                foreach (int id in DefaultTopTierPrefixes)
                {
                    _whitelist.Add(id);
                }
            }
            else
            {
                foreach (int id in configList)
                {
                    if (id > 0) _whitelist.Add(id);
                }
            }

            OnWhitelistChanged?.Invoke();
        }

        /// <summary>
        /// 导出当前白名单列表供全局设置序列化保存
        /// </summary>
        public static List<int> ExportWhitelist()
        {
            return _whitelist.ToList();
        }

        /// <summary>
        /// 获取所有原版有效前缀条目元数据（包含分类与本地化名称）
        /// </summary>
        public static List<PrefixEntry> GetAllPrefixEntries()
        {
            var entries = new List<PrefixEntry>();

            for (int id = 1; id < PrefixID.Count; id++)
            {
                string name = string.Empty;
                if (Lang.prefix != null && id < Lang.prefix.Length && Lang.prefix[id] != null)
                {
                    name = Lang.prefix[id].Value;
                }

                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                string category;
                if (id >= 62 && id <= 80)
                {
                    category = "饰品前缀";
                }
                else if (id == PrefixID.Legendary || id == PrefixID.Legendary2 || id == PrefixID.Unreal || id == PrefixID.Mythical ||
                         id == PrefixID.Godly || id == PrefixID.Demonic || id == PrefixID.Ruthless)
                {
                    category = "极品武器前缀";
                }
                else if (id >= 1 && id <= 15)
                {
                    category = "通用/近战前缀";
                }
                else if (id >= 16 && id <= 25)
                {
                    category = "远程前缀";
                }
                else if (id >= 26 && id <= 35)
                {
                    category = "魔法前缀";
                }
                else
                {
                    category = "常规前缀";
                }

                bool isTopTier = DefaultTopTierPrefixes.Contains(id);

                entries.Add(new PrefixEntry
                {
                    Id = id,
                    Name = name,
                    Category = category,
                    IsTopTierDefault = isTopTier
                });
            }

            return entries;
        }
    }
}
