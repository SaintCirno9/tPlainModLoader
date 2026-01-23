using CommandHelp;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Terraria;

namespace tContentPatch.Content
{
    [HarmonyPatch(typeof(Main), "ReadLineInput")]
    internal static class DedServConsoleCommand
    {
        public static bool Enable = false;

        private static void Postfix(ref string __result)
        {
            if (Enable == false) return;
            if (Main.dedServ == false) return;
            StackTrace st = new StackTrace();
            StackFrame sf = st?.GetFrame(2);
            string methodName = sf?.GetMethod()?.Name;
            if (methodName != nameof(Main.startDedInputCallBack)) return;

            //

            bool hasrun = TryRunCMD(__result);

            if (hasrun) __result = string.Empty;//如果运行了就返回空
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
                //解析成功

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
                Console.WriteLine($"指令运行失败:{ex.Message}");
            }
        }
    }
}
