using System;
using TPML.Core.Configuration;

namespace tContentPatch.Utils
{
    /// <summary>
    /// 配置文件访问器的旧命名空间兼容门面，实现已迁移至 TPML.Core.Configuration.ConfigStore。
    /// </summary>
    public class ConfigHelp<T>
    {
        /// <summary>
        /// 当前配置对象。
        /// </summary>
        public T config => _configStore.Config;

        private readonly ConfigStore<T> _configStore;

        /// <summary>
        /// 使用指定配置文件路径创建访问器。
        /// </summary>
        public ConfigHelp(string filePath)
        {
            _configStore = new ConfigStore<T>(filePath);
        }

        /// <summary>
        /// 加载配置；文件不存在时用 repair 创建默认值并写入。
        /// </summary>
        public void UpdateConfig(Func<T> repair = null)
        {
            _configStore.UpdateConfig(repair);
        }

        /// <summary>
        /// 保存当前配置。
        /// </summary>
        public void SaveConfig()
        {
            _configStore.SaveConfig();
        }
    }
}