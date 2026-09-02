//using System.Threading.Tasks;

//namespace TPML.Command
//{
//    internal static class Pipe
//    {
//        internal const string pipe_toTPML = "TPML_Pipe_command_ToTPML";
//        internal const string pipe_toOutput = "TPML_Pipe_command_ToOutput";
//        private static bool isEnableSend = false;
//        private static int count = 0;
//        private static bool isEnableReceive = false;

//        /// <summary>
//        /// 允许多次调用
//        /// </summary>
//        public static void Initialize(bool enable)
//        {
//            isEnableSend = enable;
//            if (isEnableSend == false) return;

//            if (isEnableReceive == false) Enable_Receive();

//            Enable_Send();
//        }

//        private static void Enable_Receive()
//        {
//            isEnableReceive = true;

//            _ = Task.Run(() =>
//            {
//                TPML.Utils.Pipe.Pipe_receive(pipe_toTContentPatch, s =>
//                {
//                    //Enable_Send();//如果收到消息就重新启用
//                    if (s == null) return;
//                    s = s.Trim();
//                    ContentPatch.RunCommand(s);
//                });
//            });
//        }

//        private static void Enable_Send()
//        {
//            ++count;
//            isEnableSend = true;
//        }

//        public static void SendMsg(string msg)
//        {
//            if (isEnableSend == false) return;

//            int nowCount = count;

//            TPML.Utils.Pipe.Pipe_send(pipe_toOutput, msg, () =>
//            {
//                //如果这次的消息超时则禁用管道
//                if (nowCount != count) return;
//                isEnableSend = false;
//            });
//        }
//    }
//}
