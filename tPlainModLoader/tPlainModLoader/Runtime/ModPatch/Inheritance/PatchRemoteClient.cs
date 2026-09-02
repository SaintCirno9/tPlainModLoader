using System;
using Terraria;

namespace tContentPatch
{
    /// <summary>
    /// 已废弃的客户端连接 Patch 基类。
    /// 作者: SaintCirno9
    /// </summary>
    [Obsolete("TPML 现代体系已全面替代 Patch*，请继承 ModSystem / ModPlayer / Global*")]
    public abstract class PatchRemoteClient
    {
        /// <summary>
        /// <see cref="Mod.Loaded"/>后调用
        /// </summary>
        public virtual void Initialize() { }
        /// <summary>
        /// 重置连接前
        /// <para/>获取离线玩家信息: if (This.IsActive) Main.player[This.Id].name
        /// </summary>
        public virtual void ResetPrefix(RemoteClient This) { }
    }
}
