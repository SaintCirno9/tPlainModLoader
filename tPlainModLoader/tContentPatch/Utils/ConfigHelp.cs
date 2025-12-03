using System;
using System.IO;

namespace tContentPatch.Utils
{
    /// <summary/>
    public class ConfigHelp<T>
    {
        /// <summary/>
        public T config { get; private set; } = default;
        private string filePath = null;

        /// <summary/>
        public ConfigHelp(string filePath)
        {
            this.filePath = filePath;
        }

        /// <summary/>
        public void UpdateConfig(Func<T> repair = null)
        {
            try
            {
                if (File.Exists(filePath) == false && repair != null)
                {
                    MyJson1.Save(repair.Invoke(), filePath);
                }

                config = MyJson1.Get2<T>(filePath);
            }
            catch { }
        }

        /// <summary/>
        public void SaveConfig()
        {
            try
            {
                MyJson1.Save(config, filePath);
            }
            catch { }
        }
    }
}
