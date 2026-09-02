using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TPML.Utils.TCPUtils;

namespace tPlainModLoaderInjector
{
    internal class CommandTCP
    {
        private static TCPC tcpc = null;

        public static void Initialize(int port)
        {
            Console.Title = TPML.Command.MsgCommand.JoinWindowTile;

            tcpc = new TCPC();
            tcpc.OnGot += s =>
            {
                if (s == null) return;
                Console.WriteLine(s);
            };

            _ = Task.Run(() =>
            {
                try
                {
                    tcpc.Start("127.0.0.1", port);
                }
                catch { }

                tcpc = null;
                Console.WriteLine("指令接收断开连接,现在无法发送和接收消息");
            });
        }

        public static void Run()
        {
            try
            {
                Console.InputEncoding = Encoding.UTF8;
                Console.OutputEncoding = Encoding.UTF8;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"设置控制台编码失败（已忽略）: {ex.Message}");
            }

            while (true)
            {
                try
                {
                    Thread.Sleep(1);

                    string s = Console.ReadLine();
                    if (s == null) continue;

                    tcpc?.SendToServer(s);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"发送消息时发生未知异常:{ex.Message}");
                }
            }
        }
    }
}
