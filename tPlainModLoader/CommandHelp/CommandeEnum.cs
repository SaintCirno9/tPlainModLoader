using System.Collections.Generic;

namespace CommandHelp
{
    public class CommandeEnum : CommandValue<int>
    {
        private int _rValue;
        private readonly string[] _enums = null;
        public string[] Enums => _enums;
        public virtual string[] TipTexts { get; set; } = null;


        public CommandeEnum(bool IsVariable, params string[] enums) : base(IsVariable)
        {
            _enums = enums;
        }


        public override object Run(ref int index, List<CommandObject> commandList)
        {
            return _rValue;
        }

        public override CommandObject Parse(string command)
        {
            if (command == "" && IsVariable)
            {
                IsDefault = true;
                _rValue = GetDefault();
                return this;
            }

            _rValue = ArgConvertThrow(command);

            return this;
        }

        protected override int ArgConvertThrow(string arg)
        {
            for (int i = 0; i < _enums.Length; ++i)
            {
                if (arg == _enums[i]) return i;
            }

            throw new Exceptions.CommandException(exceptionmessage: $"未找到与参数[{arg}]对应的值");
        }

        protected override int GetDefault()
        {
            return -1;
        }
    }
}
