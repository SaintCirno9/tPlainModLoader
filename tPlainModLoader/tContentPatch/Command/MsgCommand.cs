using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using tContentPatch.Utils;
using tContentPatch.Utils.TCPUtils;

namespace tContentPatch.Command
{
    /// <summary/>
    public class MsgCommand
    {
        /// <summary>
        /// 窗口标题是这个的才能连接
        /// </summary>
        public const string JoinWindowTile = "tPlainModLoader.MsgCommand";
        /// <summary>
        /// 接收端端口
        /// </summary>
        public static int Prot => tcps?.ListenerPort ?? -1;
        private static TCPS tcps = null;

        internal static void Initialize()
        {
            tcps = new TCPS(5);
            tcps.OnCanJoin += (ip, port) =>
            {
                Console.WriteLine($"收到指令连接[{ip}:{port}]");
                bool ok = CanJoin(ip, port);
                Console.WriteLine($"{(ok ? null : "不")}允许连接");
                return ok;
            };
            tcps.OnGotClient += s =>
            {
                if (s == null) return;
                ContentPatch.RunCommand(s);
            };

            _ = Task.Run(() =>
            {
                string s = "启动指令接收";

                try
                {
                    Log.Add(s);
                    Console.WriteLine(s);
                    tcps.Start(0);
                }
                catch { }

                tcps = null;
                s = "指令消息接收端关闭,现在无法收到指令";
                Log.Add(s);
                Console.WriteLine(s);
            });
        }

        private static bool CanJoin(string ip, int port)
        {
            try
            {
                if (ip != "127.0.0.1") return false;
                Process pr = UsePortWithProcess(port);
                if (pr == null) return false;
                if (pr.MainWindowTitle != JoinWindowTile) return false;

                return true;
            }
            catch { }

            return false;
        }

        /// <summary>
        /// 发送消息到客户端
        /// </summary>
        public static void SendMsg(string msg)
        {
            if (msg == null) return;

            tcps?.SendToClient(msg);
        }

        #region
        private static string ExecCMD(string command)
        {
            //_ = Console.InputEncoding;
            //_ = Console.OutputEncoding;
            //Console.InputEncoding = Encoding.UTF8;
            //Console.OutputEncoding = Encoding.UTF8;

            //Process pro = new Process();
            //pro.StartInfo.FileName = "cmd.exe";
            //pro.StartInfo.UseShellExecute = false;//是否使用操作系统shell启动
            //pro.StartInfo.RedirectStandardInput = true;//接受来自调用程序的输入信息
            //pro.StartInfo.RedirectStandardOutput = true;//由调用程序获取输出信息
            ////pro.StartInfo.RedirectStandardError = true;//重定向标准错误输出
            //pro.StartInfo.CreateNoWindow = true;//不显示程序窗口
            //pro.Start();
            //_ = pro.StandardOutput.CurrentEncoding;
            //_ = pro.StandardInput.Encoding;
            //Console.WriteLine(pro.StandardOutput.CurrentEncoding.Equals(pro.StandardInput.Encoding));
            ////这里使用&是批处理命令的符号,表示前面一个命令不管是否执行成功都执行后面(exit)命令
            ////如果不执行exit命令,后面调用ReadToEnd()方法会假死
            ////pro.StandardInput.WriteLine($"{command}$exit");
            ////pro.StandardInput.AutoFlush = true;
            //pro.StandardInput.WriteLine(command);
            ////pro.StandardInput.WriteLine("exit");
            //pro.StandardInput.Close();
            //string output = pro.StandardOutput.ReadToEnd();//获取cmd窗口的输出信息
            ////pro.WaitForExit();//等待程序执行完退出进程
            //pro.Close();
            //Console.WriteLine(output);
            //return output;

            //整半天还得是你ai哥
            //ProcessStartInfo processInfo = new ProcessStartInfo("cmd.exe", "/c netstat -ano");
            ProcessStartInfo processInfo = new ProcessStartInfo("cmd.exe", command);
            processInfo.RedirectStandardOutput = true;
            processInfo.UseShellExecute = false;
            processInfo.CreateNoWindow = true;

            try
            {
                // 设置 StandardOutputEncoding 为代码页 936（简体中文）
                processInfo.StandardOutputEncoding = Encoding.GetEncoding("GB2312");
                //processInfo.StandardOutputEncoding = Encoding.UTF8;
            }
            catch { }

            using (Process process = Process.Start(processInfo))
            {
                using (StreamReader reader = process.StandardOutput)
                {
                    string result = reader.ReadToEnd();
                    //Console.WriteLine(result);

                    return result;
                }
            }
        }

        private static Process UsePortWithProcess(int port)
        {
            try
            {
                //执行多事获取端口信息
                string cmd_response = ExecCMD("/c netstat -ano");
                byte[] txt = Encoding.UTF8.GetBytes(cmd_response.ToCharArray());
                using (Stream readStream = new MemoryStream(txt))
                {
                    readStream.Position = 0;
                    using (StreamReader reader = new StreamReader(readStream))
                    {
                        //正则表达式 用于提取信息
                        Regex reg = new Regex(" \\s+ ", RegexOptions.Compiled);
                        string line = null;
                        while ((line = reader.ReadLine()) != null)
                        {
                            line = line.Trim();
                            //提取需要的端口相关信息行
                            if (line.StartsWith("TCP", StringComparison.OrdinalIgnoreCase))
                            {
                                line = reg.Replace(line, ",");
                                string[] arr = line.Split(',');
                                if (arr[1].EndsWith($":{port}"))
                                {
                                    int pid = int.Parse(arr[4]);
                                    Process p = Process.GetProcessById(pid);
                                    return p;
                                }
                            }
                        }
                    }
                }
            }
            catch { }

            return null;
        }
        #endregion
    }
}
