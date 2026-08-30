using CommandHelp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Terraria;
using TPML.Content.Engine;

namespace tContentPatch.Content
{
    /// <summary>
    /// 服务端控制台指令（M2 迁移：Harmony → MonoMod，静态方法 + __result）
    /// </summary>
    internal static class DedServConsoleCommand
    {
        public static bool Enable = false;

        /// <summary>集中注册全部补丁（由 ContentPatch_Initialize 调用）</summary>
        public static void RegisterAll()
        {
            // Main.ReadLineInput()（静态，返回 string）
            HookRegistry.Add(typeof(Main).GetMethod("ReadLineInput", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic),
                (Func<Func<string>, string>)(orig =>
                {
                    string result = orig();
                    Postfix(ref result);
                    return result;
                }));
        }

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
                ContentPatch.PrintTry($"指令运行失败:{ex.Message}");
            }
        }
    }
}
