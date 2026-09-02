using System.Collections.Generic;
using System.IO;

namespace TPML.Network
{
    internal static class ModNetworkPacket
    {
        private static readonly Dictionary<string, ModNetPacket> kv = new Dictionary<string, ModNetPacket>();

        public static void Clear()
        {
            kv.Clear();
        }

        public static void Register(List<ModNetPacket> mod)
        {
            mod?.ForEach(i => kv.Add(i.key, i));
        }

        public static void Deserialize(BinaryReader reader, int userId)
        {
            string key = reader.ReadString();
            if (kv.ContainsKey(key) == false) return;

            kv[key].Deserialize(reader, userId);
        }

        public static void OnGetNotice(int userId)
        {
            foreach (KeyValuePair<string, ModNetPacket> i in kv)
            {
                try
                {
                    i.Value.OnGetNotice(userId);
                }
                catch { }
            }
        }
    }
}
