using CommandHelp.Exceptions;
using System;
using System.Collections.Generic;

namespace CommandHelp
{
    public class ParseCommand
    {
        /// <summary>
        /// 返回解析成功部分的指令对象和指令异常
        /// </summary>
        /// <param name="command"></param>
        /// <param name="cmdList"></param>
        /// <returns></returns>
        public static (List<CommandObject> cos, CommandException cex) Parse(string command, List<CommandObject> cmdList)
        {
            return parse(command, 0, cmdList, new List<CommandObject>());
        }

        private static (List<CommandObject> cos, CommandException cex) parse(string command, int cmdIndex, List<CommandObject> cmdList, List<CommandObject> parseList)
        {
            //if (command == null) return (parseList, null);
            //if (command.Length == 0) return (parseList, null);
            if (cmdList == null) return (parseList, new CommandException(exceptionmessage: "未知异常"));

            //

            for (int i = 0; i < cmdList.Count; ++i)
            {
                CommandObject co = cmdList[i];
                if (co == null) continue;

                (string cmdParse, string cmd) v;

                try
                {
                    v = co.ParseFormat(command);
                    if (v.cmdParse == null) continue;

                    CommandObject subCo = co.Parse(v.cmdParse);
                    if (subCo == null) continue;
                }
                catch (CommandException cex)
                {
                    return (parseList, new CommandException(cmdIndex, command, cex.ExceptionMessage, cex));
                }
                catch (Exception ex)
                {
                    return (parseList, new CommandException(cmdIndex, command, "未知异常", ex));
                }

                parseList.Add(co);

                cmdIndex += command.Length - v.cmd.Length;

                return parse(v.cmd, cmdIndex, co.SubCommand, parseList);
            }

            CommandException ce = null;
            bool isEmpty = command.TrimStart(' ').Length < 1;

            if (cmdList.Count > 0)
            {
                //添加失败原因, 空指令为指令缺失, 否则解析失败
                if (isEmpty)
                    ce = new CommandLackException(cmdIndex, command);
                else
                    ce = new CommandParseException(cmdIndex, command);
            }
            else
            {
                //添加失败原因, 指令不为空且没有子命令时
                if (isEmpty == false) ce = new CommandParseException(cmdIndex, command);
            }

            return (parseList, ce);
        }
    }
}
