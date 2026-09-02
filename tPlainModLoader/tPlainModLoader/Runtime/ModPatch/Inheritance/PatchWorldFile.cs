using System;

namespace tContentPatch
{
    /// <summary>
    /// 世界存档兼容基类（建议直接继承 <see cref="TPML.Content.ModSystem"/> 并使用 HookGen 门面或 Sidecar）。
    /// 作者: SaintCirno9
    /// </summary>
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
