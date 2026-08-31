using CommandHelp;
using System;
using System.Collections.Generic;

namespace tContentPatch.Command
{
    internal class ProgramCommand
    {
        /// <summary>
        /// 用已有的指令列表运行指令
        /// </summary>
        /// <param name="command"></param>
        public static void Run(string command)
        {
            if (string.IsNullOrWhiteSpace(command)) return;

            string cmd = command.Trim();
            if (cmd.StartsWith("/") || cmd.StartsWith("."))
            {
                cmd = cmd.Substring(1).Trim();
            }

            List<CommandObject> cos = GetCos();

            string msg = Utils.CommandRun(cmd, cos);
            if (msg == null) return;
            
            ContentPatch.PrintTry(msg);
        }

        /// <summary>
        /// 尝试识别并运行指令。若首个关键词匹配已知根指令则执行并返回 true，否则返回 false。
        /// </summary>
        public static bool TryRun(string command)
        {
            if (string.IsNullOrWhiteSpace(command)) return false;

            string cmd = command.Trim();
            if (cmd.StartsWith("/") || cmd.StartsWith("."))
            {
                cmd = cmd.Substring(1).Trim();
            }

            if (string.IsNullOrWhiteSpace(cmd)) return false;

            List<CommandObject> cos = GetCos();
            string firstToken = cmd.Split(' ')[0];

            bool isKnown = false;
            foreach (var co in cos)
            {
                if (co != null && string.Equals(co.Text, firstToken, StringComparison.OrdinalIgnoreCase))
                {
                    isKnown = true;
                    break;
                }
            }

            if (!isKnown) return false;

            string msg = Utils.CommandRun(cmd, cos);
            if (msg != null)
            {
                ContentPatch.PrintTry(msg);
                try
                {
                    if (Terraria.Main.netMode == 0 || Terraria.Main.netMode == 1)
                    {
                        Terraria.Main.NewText(msg, 255, 100, 100);
                    }
                }
                catch { }
            }

            return true;
        }

        public static List<CommandObject> GetCos()
        {
            List<CommandObject> cos = GetCO();

            List<CommandObject> cosMod = GetModCO();
            if (cosMod != null) cos.AddRange(cosMod);

            cos.Add(Utils.GetCO_OutputCOList(cos));

            return cos;
        }

        private static List<CommandObject> GetCO()
        {
            List<CommandObject> cos = new List<CommandObject>();

            CommandObject console = new CommandObject("console");
            CommandMethod console_clear = new CommandMethod("clear");
            console_clear.Runing += args =>
            {
                tContentPatch.Utils.ConsoleUtils.Clear();
            };
            console.SubCommand.Add(console_clear);
            console.SubCommand.Add(Utils.GetCO_OutputCOList(console.SubCommand));

            cos.Add(console);
            cos.Add(ProfileCommand.CreateCommand());

            return cos;
        }

        private static List<CommandObject> GetModCO()
        {
            try
            {
                List<ModLoad.ModObject> mos = ContentPatch.GetModObjects();
                if (mos == null) return null;

                List<CommandObject> cos_mod = new List<CommandObject>();

                foreach (ModLoad.ModObject mo in mos)
                {
                    for (int i = 0; i < mo?.inheritance_mod?.Count; ++i)
                    {
                        List<CommandObject> cosMod = mo.inheritance_mod[i].GetCommands();
                        if (cosMod == null) continue;
                        cos_mod.AddRange(cosMod);
                    }
                }

                return cos_mod;
            }
            catch (Exception ex)
            {
                ContentPatch.PrintTry($"获取模组指令列表失败:{ex.Message}");
                return null;
            }
        }
    }
}
