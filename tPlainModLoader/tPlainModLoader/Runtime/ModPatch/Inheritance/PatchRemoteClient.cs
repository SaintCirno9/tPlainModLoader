using System;
using Terraria;

namespace tContentPatch
{
    /// <summary>
    /// 客户端连接兼容基类。
    /// 作者: SaintCirno9
    /// </summary>
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
