using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace TPML.Utils.TCPUtils
{
    /// <summary>
    /// 服务端
    /// </summary>
    public class TCPS
    {
        /// <summary>
        /// 远程客户端
        /// </summary>
        public class RemoteClient
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

            /// <summary>
            /// 重置连接
            /// </summary>
            public void Reset(TcpClient c = null)
            {
                try
                {
                    task?.Dispose();
                    writer = null;
                    Client?.Close();

                    Client = c;

                    if (Connected == false) return;

                    writer = new BinaryWriter(Client.GetStream());

                    if (task == null) StartGot();
                }
                catch
                {
                    Client?.Close();
                    Client = null;
                }
            }

            private void StartGot()
            {
                task?.Dispose();

                task = Task.Run(() =>
                {
                    try
                    {
                        BinaryReader br = new BinaryReader(Client.GetStream());

                        while (true)
                        {
                            string msg = br.ReadString();
                            OnGot?.Invoke(msg);
                        }
                    }
                    catch
                    {
                        task = null;
                    }
                });
            }

            /// <exception cref="Exception"/>
            public void SendToClient(string msg)
            {
                writer.Write(msg);
            }
        }

        /// <summary/>
        public Action<string> OnGotClient = null;
        /// <summary/>
        public Func<string, int, bool> OnCanJoin = null;
        /// <summary/>
        public TcpListener listener { get; protected set; } = null;
        /// <summary/>
        public int ListenerPort { get; protected set; } = -1;
        /// <summary/>
        public RemoteClient[] Clients { get; private set; } = null;

        /// <summary/>
        public TCPS(int count = 0)
        {
            Clients = new RemoteClient[count];
            for (int i = 0; i < Clients.Length; ++i)
            {
                Clients[i] = new RemoteClient();
                Clients[i].OnGot += s => OnGotClient?.Invoke(s);
            }
        }

        /// <exception cref="SocketException"/>
        public void Start(int port = 0)
        {
            ListenerPort = -1;
            listener?.Stop();

            try
            {
                listener = new TcpListener(IPAddress.Any, port);
                listener.Start();

                ListenerPort = ((IPEndPoint)listener.LocalEndpoint).Port;

                StartListener(listener);
            }
            catch (Exception ex)
            {
                ListenerPort = -1;
                listener?.Stop();
                throw ex;
            }
        }

        /// <summary>
        /// 添加连接的客户端, 失败返回<see langword="false"/>
        /// </summary>
        public bool AddConnectedClient(TcpClient client)
        {
            if (listener == null) return false;
            if (listener.Server.Connected) return false;
            if (client == null) return false;
            if (client.Connected == false) return false;

            foreach (RemoteClient c in Clients)
            {
                if (c.Connected) continue;

                c.Reset(client);
                return true;
            }

            return false;
        }

        private void StartListener(TcpListener listener)
        {
            while (listener == this.listener)
            {
                TcpClient client = null;

                try
                {
                    client = listener.AcceptTcpClient();
                    IPEndPoint ipp = (IPEndPoint)client.Client.RemoteEndPoint;
                    string ip = ipp.Address.ToString();
                    int port = ipp.Port;

                    if (OnCanJoin?.Invoke(ip, port) == false)
                    {
                        client.Close();
                        continue;
                    }

                    bool add = AddConnectedClient(client);
                    if (add == false)
                    {
                        client.Close();
                        continue;
                    }
                }
                catch
                {
                    client?.Close();
                    System.Threading.Thread.Sleep(1);
                }
            }
        }

        /// <summary/>
        public void SendToClient(string msg)
        {
            if (msg == null) return;

            foreach (RemoteClient client in Clients)
            {
                if (client.Connected == false) continue;

                try
                {
                    client.SendToClient(msg);
                }
                catch
                {
                    client.Reset();
                }
            }
        }
    }
}
