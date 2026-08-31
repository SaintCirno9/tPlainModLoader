namespace tContentPatch
{
    /// <summary/>
    public abstract class PatchWorldFile
    {
        /// <summary>
        /// <see cref="Mod.Loaded"/>后调用
        /// </summary>
        public virtual void Initialize() { }
        /// <summary>
        /// 保存世界前, 单人和服务端有效
        /// </summary>
        public virtual void SaveWorldPrefix(bool useCloudSaving, bool resetTime, bool useTemps, bool canBeSkipped) { }
        /// <summary>
        /// 保存世界后, 单人和服务端有效
        /// </summary>
        public virtual void SaveWorldPostfix(bool useCloudSaving, bool resetTime, bool useTemps, bool canBeSkipped) { }
        /// <summary>
        /// 加载世界后, 单人和服务端有效
        /// </summary>
        public virtual void LoadWorldPostfix() { }
    }
}
