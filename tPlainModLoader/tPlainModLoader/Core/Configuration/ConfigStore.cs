using System;
using TPML.Core.Json;

namespace TPML.Core.Configuration
{
    /// <summary>
    /// 配置文件加载与保存门面。
    /// </summary>
    public class ConfigStore<T>
    {
        /// <summary>
        /// 当前配置对象。
        /// </summary>
        public T Config { get; private set; } = default;

        private readonly string _filePath;

        /// <summary>
        /// 使用指定配置文件路径创建存储。
        /// </summary>
        public ConfigStore(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("文件路径不能为空", nameof(filePath));
            _filePath = filePath;
        }

        /// <summary>
        /// 加载配置；文件不存在时用 repair 创建默认值并写入。
        /// </summary>
        public void UpdateConfig(Func<T> repair = null)
        {
            try
            {
                if (!System.IO.File.Exists(_filePath) && repair != null)
                {
                    JsonHelper.Save(repair.Invoke(), _filePath);
                }

                Config = JsonHelper.Get2<T>(_filePath);
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"[ConfigStore] 加载配置失败 ({_filePath}): {ex.Message}");
                if (repair != null)
                {
                    try
                    {
                        Config = repair.Invoke();
                        JsonHelper.Save(Config, _filePath);
                    }
                    catch (Exception repairEx)
                    {
                        System.Console.WriteLine($"[ConfigStore] 修复配置失败: {repairEx.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// 保存当前配置。
        /// </summary>
        public void SaveConfig()
        {
            try
            {
                JsonHelper.Save(Config, _filePath);
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"[ConfigStore] 保存配置失败 ({_filePath}): {ex.Message}");
            }
        }
    }
}