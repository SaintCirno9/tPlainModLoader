using System.Collections.Generic;

namespace CommandHelp
{
    public class CommandObject
    {
        public virtual string Text { get; } = null;
        public virtual string TipText { get; set; } = null;
        public List<CommandObject> SubCommand { get; } = new List<CommandObject>();


        public CommandObject(string texe = null)
        {
            Text = texe;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="index"></param>
        /// <param name="commandList">解析后的指令对象</param>
        /// <returns></returns>
        public virtual object Run(ref int index, List<CommandObject> commandList)
        {
            return null;
        }

        /// <summary>
        /// 解析一句指令, 返回解析成功的对象
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public virtual CommandObject Parse(string command)
        {
            return command == Text ? this : null;
        }

        /// <summary>
        /// 解析一段指令, 按格式分出属于该指令的部分和不属于的部分, 返回解析成功部分和未解析部分
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public virtual (string cmdParse, string cmd) ParseFormat(string command)
        {
            if (command == null) return (null, command);

            char key = ' ';
            string old = command;

            //"head subCommamd", 获取"head"部分

            //清空开头空格
            command = command.TrimStart(key);

            if (command.Length == 0) return (null, old);

            //获取"head"之后空格位置
            int index = command.IndexOf(key);

            //获取"head"长度
            int headLen = index;
            if (headLen < 1) headLen = command.Length;

            //获取"head"
            string head = command.Substring(0, headLen);

            //获取剩余指令
            command = command.Remove(0, headLen);

            return (head, command);
        }
    }
}
