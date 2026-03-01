using Microsoft.Xna.Framework;
using System;
using System.IO;
using Terraria;
using Terraria.ID;

namespace PixelArt.Content.Utils
{
    internal static class ClientSendToServer
    {
        public static void TrySend(byte msgType, Action<BinaryWriter> action)
        {
            try
            {
                Send(msgType, action);
            }
            catch { }
        }

        public static void Send(byte msgType, Action<BinaryWriter> action)
        {
            if (Main.netMode != 1) return;
            if (Netplay.Connection.IsConnected() == false) return;

            MessageBuffer buffer = NetMessage.buffer[256];

            lock (buffer)
            {
                if (buffer.writer == null) buffer.ResetWriter();
                BinaryWriter writer = buffer.writer;
                writer.BaseStream.Position = 0L;
                long position = writer.BaseStream.Position;
                writer.BaseStream.Position += 2L;
                writer.Write((byte)msgType);

                action(writer);

                int size = (int)writer.BaseStream.Position;
                if (size > 65535) return;

                writer.BaseStream.Position = position;
                writer.Write((ushort)size);
                writer.BaseStream.Position = size;

                try
                {
                    ushort num = BitConverter.ToUInt16(buffer.writeBuffer, 0);

                    Netplay.Connection.Socket.AsyncSend(buffer.writeBuffer, 0, num, Netplay.Connection.ClientWriteCallBack);
                }
                catch { }
            }
        }

        public static void SendSyncEquipment(int type, int slot, int whoAmi)
        {
            TrySend(MessageID.SyncEquipment, writer =>
            {
                writer.Write((byte)whoAmi);
                writer.Write((short)slot);
                Item item = new Item();
                item.SetDefaults(type);
                item.stack = 1;
                writer.Write((short)item.stack);
                writer.Write(item.prefix);
                writer.Write((short)item.type);
                BitsByte bb = default;
                bb[0] = item.favorited;
                bb[1] = false;
                writer.Write(bb);
            });
        }

        public static void SendPlayerControls(int whoAmi, Vector2 pos)
        {
            TrySend(MessageID.PlayerControls, writer =>
            {
                Player player = Main.player[whoAmi];
                writer.Write((byte)whoAmi);
                BitsByte bb23 = (byte)0;
                bb23[0] = player.controlUp;
                bb23[1] = player.controlDown;
                bb23[2] = player.controlLeft;
                bb23[3] = player.controlRight;
                bb23[4] = player.controlJump;
                bb23[5] = player.controlUseItem;
                bb23[6] = player.direction == 1;
                writer.Write(bb23);
                BitsByte bb24 = (byte)0;
                bb24[0] = player.pulley;
                bb24[1] = player.pulley && player.pulleyDir == 2;
                bb24[2] = player.velocity != Vector2.Zero;
                bb24[3] = player.vortexStealthActive;
                bb24[4] = player.gravDir == 1f;
                bb24[5] = player.shieldRaised;
                bb24[6] = player.ghost;
                bb24[7] = player.mount.Active;
                writer.Write(bb24);
                BitsByte bb25 = (byte)0;
                bb25[0] = player.tryKeepingHoveringUp;
                bb25[1] = player.IsVoidVaultEnabled;
                bb25[2] = player.sitting.isSitting;
                bb25[3] = player.downedDD2EventAnyDifficulty;
                bb25[4] = player.petting.isPetting;
                bb25[5] = player.petting.isPetSmall;
                bb25[6] = player.PotionOfReturnOriginalUsePosition.HasValue;
                bb25[7] = player.tryKeepingHoveringDown;
                writer.Write(bb25);
                BitsByte bb26 = (byte)0;
                bb26[0] = player.sleeping.isSleeping;
                bb26[1] = player.autoReuseAllWeapons;
                bb26[2] = player.controlDownHold;
                bb26[3] = player.isOperatingAnotherEntity;
                bb26[4] = player.controlUseTile;
                bb26[5] = false;
                bb26[6] = player.lastItemUseAttemptSuccess;
                writer.Write(bb26);
                writer.Write((byte)player.selectedItem);
                writer.WriteVector2(pos);
                if (bb24[2])
                {
                    writer.WriteVector2(player.velocity);
                }
                if (bb24[7])
                {
                    writer.Write((ushort)player.mount.Type);
                }
                if (bb25[6])
                {
                    writer.WriteVector2(player.PotionOfReturnOriginalUsePosition.Value);
                    writer.WriteVector2(player.PotionOfReturnHomePosition.Value);
                }
            });
        }
    }
}
