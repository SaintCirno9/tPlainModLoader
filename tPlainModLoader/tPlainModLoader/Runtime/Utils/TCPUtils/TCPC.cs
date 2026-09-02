using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace TPML.Utils.TCPUtils
{
    /// <summary>
    /// 客户端
    /// </summary>
    public class TCPC
    {
        /// <summary/>
        public Action<string> OnGot = null;
        /// <summary/>
        public TcpClient Client { get; protected set; } = null;
        /// <summary/>
        public bool Connected => Client?.Connected == true;
        /// <summary/>
        public BinaryWriter writer { get; protected set; } = null;
        private Task task = null;

        /// <exception cref="Exception"/>
        public void Start(string ip, int port)
        {
            try
            {
                Stop();

                Client = new TcpClient();
                Client.Connect(ip, port);

                writer = new BinaryWriter(Client.GetStream());

                StartListener(Client);
            }
            catch { }
            finally
            {
                Stop();
            }
        }

        /// <summary/>
        public void Stop()
        {
            try
            {
                task?.Dispose();
            }
            catch { }
            task = null;
            writer = null;
            Client?.Close();
        }

        private void StartListener(TcpClient client)
        {
            BinaryReader br = new BinaryReader(client.GetStream());

            while (client == Client)
            {
                string msg = br.ReadString();
                OnGot?.Invoke(msg);
            }
        }

        /// <summary/>
        public void SendToServer(string msg)
        {
            try
            {
                if (Connected == false) return;
                if (msg == null) return;
                writer.Write(msg);
            }
            catch { }
        }
    }
}
