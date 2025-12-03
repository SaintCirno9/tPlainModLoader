using Microsoft.Xna.Framework;

namespace tContentPatch
{
    /// <summary/>
    public abstract class PatchRemadeChatMonitor
    {
        /// <summary>
        /// <see cref="Mod.Loaded"/>后调用
        /// </summary>
        public virtual void Initialize() { }
        /// <summary/>
        public virtual void DrawChatPostfix(bool drawingPlayerChat) { }
        /// <summary/>
        public virtual void AddNewMessagePrefix(ref string text, Color color, int widthLimitInPixels = -1) { }
    }
}
