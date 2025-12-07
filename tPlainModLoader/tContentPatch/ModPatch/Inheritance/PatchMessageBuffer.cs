using Terraria;

namespace tContentPatch
{
    /// <summary/>
    public abstract class PatchMessageBuffer
    {
        /// <summary>
        /// <see cref="Mod.Loaded"/>后调用
        /// </summary>
        public virtual void Initialize() { }
        /// <summary>
        /// <see cref="MessageBuffer.GetData(int, int, out int)"/>前调用
        /// </summary>
        public virtual void GetDataPrefix(MessageBuffer This, int start, int length, int messageType) { }
        /// <summary>
        /// <see cref="MessageBuffer.GetData(int, int, out int)"/>后调用
        /// </summary>
        public virtual void GetDataPostfix(MessageBuffer This, int start, int length, int messageType) { }
        /// <summary>
        /// 客户端收到玩家连接//只是有玩家处于活动状态时, 其它数据可能还未同步
        /// </summary>
        public virtual void OnPlayerConnect(int playerIndex) { }
        /// <summary>
        /// 客户端收到玩家断开连接//有玩家处于非活动状态时
        /// </summary>
        public virtual void OnPlayerDisconnect(int playerIndex) { }
    }
}
