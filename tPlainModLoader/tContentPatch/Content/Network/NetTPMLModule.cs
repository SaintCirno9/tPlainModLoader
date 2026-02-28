using System.Collections.Generic;
using System.IO;
using tContentPatch.ModLoad;
using Terraria;
using Terraria.Net;

namespace tContentPatch.Content.Network
{
    internal class NetTPMLModule : NetModule
    {
        private static class MsgTypeID
        {
            public const int Notice = 0;
            public const int ModPackte = 1;
        }

        public override bool Deserialize(BinaryReader reader, int userId)
        {
            int msgType = reader.ReadInt32();

            switch (msgType)
            {
                case MsgTypeID.Notice: DeserializeNotice(reader, userId); break;
                case MsgTypeID.ModPackte: ModNetworkPacket.Deserialize(reader, userId); break;
                default: return true;
            }

            return true;
        }

        private static void DeserializeNotice(BinaryReader reader, int userId)
        {
            string text = reader.ReadString();
            if (Main.dedServ == false) return;
            if (text != nameof(tContentPatch)) return;

            ModNetworkPacket.OnGetNotice(userId);
        }

        internal static void SendToServer()
        {
            if (Main.netMode != 1) return;

            NetPacket packet = CreatePacket<NetTPMLModule>();
            packet.Writer.Write(MsgTypeID.Notice);
            packet.Writer.Write(nameof(tContentPatch));

            NetManager.Instance.SendToServer(packet);
        }

        internal static NetPacket CreateModPacket(string key)
        {
            NetPacket packet = CreatePacket<NetTPMLModule>();
            packet.Writer.Write(MsgTypeID.ModPackte);
            packet.Writer.Write(key);

            return packet;
        }
    }
}
