using System;
using System.Collections.Generic;

namespace CommandHelp
{
    public class CommandMethod : CommandObject
    {
        private readonly int _argCount = 0;
        public int ArgCount => _argCount;
        public event Action<object[]> Runing = null;


        public CommandMethod(string text = null, int argCount = 0) : base(text)
        {
            if (argCount < 0) throw new ArgumentException("参数不能小于0", nameof(argCount));
            _argCount = argCount;
        }


        public override object Run(ref int index, List<CommandObject> commandList)
        {
            if (HasPrintList(ref index, commandList)) return null;

            //

            if (commandList == null) throw new ArgumentNullException(nameof(commandList));
            int rightCount = commandList.Count - index - 1;
            if (_argCount > rightCount) throw new Exceptions.CommandException(exceptionmessage: $"缺少{_argCount - rightCount}个参数");

            object[] args = new object[_argCount];

            for (int i = 0; i < _argCount; ++i)
            {
                ++index;

                object arg = commandList[index].Run(ref index, commandList);
                args[i] = arg;
            }

            return OnRuning(ref index, commandList, args);
        }

        public virtual object OnRuning(ref int index, List<CommandObject> commandList, object[] args)
        {
            Runing?.Invoke(args);

            return null;
        }

        public virtual bool HasPrintList(ref int index, List<CommandObject> commandList)
        {
            for (int i = 0;
                i < ArgCount &&
                i + index + 1 < commandList.Count;
                ++i)
            {
                if (commandList[index + i + 1] is CommandPrintList cpl == false) continue;

                index += i + 1;
                cpl.Run(ref index, commandList);
                return true;
            }

            return false;
        }
    }
}
