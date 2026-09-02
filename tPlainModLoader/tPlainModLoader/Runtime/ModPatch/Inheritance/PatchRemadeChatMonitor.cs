using System;
using Microsoft.Xna.Framework;

namespace tContentPatch
{
    /// <summary>
    /// 聊天监视器兼容基类（建议直接继承 <see cref="TPML.Content.ModSystem"/> 并使用 HookGen 门面）。
    /// 作者: SaintCirno9
    /// </summary>
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
