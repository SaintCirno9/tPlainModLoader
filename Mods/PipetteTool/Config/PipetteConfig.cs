using System;

namespace PipetteTool.Config
{
    /// <summary>
    /// 吸管工具全局配置项
    /// </summary>
    public static class PipetteConfig
    {
        /// <summary>
        /// 绑定的快捷键（默认为 "Q"）
        /// </summary>
        public static string KeyBind = "Q";

        /// <summary>
        /// 是否启用吸管工具
        /// </summary>
        public static bool Enable = true;

        /// <summary>
        /// 是否允许吸取背景墙（当鼠标位置无激活物块时）
        /// </summary>
        public static bool PickWall = true;

        /// <summary>
        /// 是否显示头顶/屏幕状态提示文本
        /// </summary>
        public static bool ShowNotification = true;

        /// <summary>
        /// 配置变更事件
        /// </summary>
        public static Action OnConfigChanged;
    }

    /// <summary>
    /// JSON 配置序列化数据结构
    /// </summary>
    [Serializable]
    public class PipetteConfigData
    {
        public string keyBind = "Q";
        public bool enable = true;
        public bool pickWall = true;
        public bool showNotification = true;
    }
}
