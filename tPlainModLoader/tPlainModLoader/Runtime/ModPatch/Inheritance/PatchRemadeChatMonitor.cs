using System;
using Microsoft.Xna.Framework;

namespace tContentPatch
{
    /// <summary>
    /// 已废弃的聊天监视器 Patch 基类。请迁移至 <see cref="TPML.Content.ModSystem"/> 并使用 HookGen 门面。
    /// 作者: SaintCirno9
    /// </summary>
    [Obsolete("TPML 现代体系已全面替代 Patch*，请继承 ModSystem / ModPlayer / Global*")]
    public abstract class PatchRemadeChatMonitor
    {
        /// <summary>
        /// <see cref="Mod.Loaded"/>后调用
        /// </summary>
        public virtual void Initialize() { }
        /// <summary/>
        public virtual void DrawChatPrefix(bool drawingPlayerChat) { }
        /// <summary/>
        public virtual void DrawChatPostfix(bool drawingPlayerChat) { }
        /// <summary/>
        public virtual void AddNewMessagePrefix(ref string text, Color color, int widthLimitInPixels = -1) { }
    }
}
