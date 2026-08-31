namespace tContentPatch
{
    /// <summary/>
    public class InfoList
    {
        /// <summary/>
        public class Directorys
        {
            /// <summary/>
            public const string Mods = "Mods";
            /// <summary>用户文档数据根目录名</summary>
            public const string UserDataRoot = "tPlainModLoader";
            /// <summary>模组配置与数据存储目录名</summary>
            public const string Config = "Config";
        }

        /// <summary/>
        public class Files
        {
            /// <summary>统一模组启用配置文件 (对齐 tML 规范)</summary>
            public const string EnabledJson = "enabled.json";
            /// <summary/>
            public const string ModLoadConfig = "loadConfig.json";
            /// <summary/>
            public const string ModInfo = "info.json";
            /// <summary/>
            public const string ModIco = "ico.png";
        }
    }
}
