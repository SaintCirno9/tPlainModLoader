using System;

namespace tContentPatch
{
    /// <summary>
    /// 已废弃的世界存档 Patch 基类。请迁移至 <see cref="TPML.Content.ModSystem"/> 并使用 HookGen 门面或 Sidecar。
    /// 作者: SaintCirno9
    /// </summary>
    [Obsolete("TPML 现代体系已全面替代 Patch*，请继承 ModSystem / ModPlayer / Global*")]
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
