using CommandHelp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Terraria;

namespace TPML.Content
{
    /// <summary>
    /// 服务端控制台指令强类型门面调度类
    /// 作者: SaintCirno9
    /// </summary>
    internal static class DedServConsoleCommand
    {
        public static bool Enable = false;
        private static bool _hooksInitialized = false;

        /// <summary>集中注册 DedServConsoleCommand 强类型 HookGen 钩子</summary>
        public static void RegisterAll()
        {
            if (_hooksInitialized) return;

            On_Main.ReadLineInput += Hook_ReadLineInput;

            _hooksInitialized = true;
        }

        private static string Hook_ReadLineInput(On_Main.orig_ReadLineInput orig)
        {
            string result = orig();
            Postfix(ref result);
            return result;
        }

        private static void Postfix(ref string __result)
        {
            if (Enable == false) return;
            if (Main.dedServ == false) return;
            StackTrace st = new StackTrace();
            StackFrame sf = st?.GetFrame(2);
            string methodName = sf?.GetMethod()?.Name;
            if (methodName != nameof(Main.startDedInputCallBack)) return;

            bool hasrun = TryRunCMD(__result);

            if (hasrun) __result = string.Empty;
        }

        private static bool TryRunCMD(string text)
        {
            if (text == null) return false;

            List<CommandObject> cos = Command.ProgramCommand.GetCos();
            if (cos == null) return false;

            foreach (CommandObject i in cos)
            {
                if (i == null) continue;
                (string cmdParse, _) = i.ParseFormat(text);
                if (cmdParse == null) continue;
                if (i.Parse(cmdParse) == null) continue;

                RunCmd(text);
                return true;
            }

            return false;
        }

        private static void RunCmd(string text)
        {
            try
            {
                ContentPatch.RunCommand(text);
            }
            catch (Exception ex)
            {
                ContentPatch.PrintTry($"指令运行失败:{ex.Message}");
            }
        }
    }
}
