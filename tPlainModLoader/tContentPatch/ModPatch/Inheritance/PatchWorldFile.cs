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
        /// 保存世界后
        /// </summary>
        public virtual void SaveWorldPostfix(bool useCloudSaving, bool resetTime = false) { }
    }
}
