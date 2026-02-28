using System.IO;
using tContentPatch.Content.Network;
using Terraria.Net;

namespace tContentPatch
{
    /// <summary/>
    public abstract class ModNetPacket
    {
        internal readonly string key = null;

        /// <summary/>
        public ModNetPacket()
        {
            key = GetType().FullName;
        }

        /// <summary>
        /// <see cref="Mod.Loaded"/>后调用
        /// </summary>
        public virtual void Initialize() { }
        /// <summary>
        /// 发送消息时
        /// </summary>
        public abstract void Serialization(BinaryWriter reader);
        /// <summary>
        /// 收到消息时
        /// </summary>
        public abstract void Deserialize(BinaryReader reader, int userId);
        /// <summary>
        /// 收到通知时, 在服务端调用, 说明该玩家用了这个加载器的客户端
        /// </summary>
        public virtual void OnGetNotice(int userId) { }

        /// <summary>
        /// 发送到服务端
        /// </summary>
        public void SendToServer()
        {
            if (RegisterNetModule.Loaded == false) return;
            NetPacket packet = NetTPMLModule.CreateModPacket(key);

            Serialization(packet.Writer);

            NetManager.Instance.SendToServer(packet);
        }
        /// <summary>
        /// 发送到客户端
        /// </summary>
        public void SendToClient(int playerId)
        {
            if (RegisterNetModule.Loaded == false) return;
            NetPacket packet = NetTPMLModule.CreateModPacket(key);

            Serialization(packet.Writer);

            NetManager.Instance.SendToClient(packet, playerId);
        }
    }
}
