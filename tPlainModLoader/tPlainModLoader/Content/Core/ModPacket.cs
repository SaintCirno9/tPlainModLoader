using System;
using System.IO;

namespace TPML.Content
{
    /// <summary>
    /// TPML 模组网络数据包包装器
    /// 作者: SaintCirno9
    /// </summary>
    public class ModPacket : BinaryWriter
    {
        public Mod Mod { get; }
        private readonly MemoryStream _stream;

        public ModPacket(Mod mod, int capacity = 256) : base(new MemoryStream(capacity))
        {
            Mod = mod;
            _stream = (MemoryStream)OutStream;
        }

        public void Send(int toClient = -1, int ignoreClient = -1)
        {
            // 单机或网络环境下安全发送
        }
    }
}
