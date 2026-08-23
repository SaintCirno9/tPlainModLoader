using System;
using System.Collections.Generic;

namespace CommandHelp
{
    public abstract class CommandValue : CommandObject
    {
        /// <summary>
        /// 是否可变, 可以不写
        /// </summary>
        public readonly bool IsVariable;
        /// <summary>
        /// 是可变的, 指示该参数未写, 是默认值
        /// </summary>
        public bool IsDefault { get; protected set; } = false;

        public CommandValue(bool isVariable = false, string text = null) : base(text)
        {
            IsVariable = isVariable;
        }
    }

    public abstract class CommandValue<T> : CommandValue
    {
        public virtual string TypeName { get; } = null;
        private T _rValue = default;


        public CommandValue(bool isVariable = false, string text = null) : base(isVariable, text) { }


        protected abstract T ArgConvertThrow(string arg);
        protected abstract T GetDefault();


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

            //值类型解析不出来就直接报错

            try
            {
                _rValue = ArgConvertThrow(command);
            }
            catch (Exception ex)
            {
                throw new Exceptions.CommandException(exceptionmessage: $"参数[{command}]类型不为<{TypeName ?? typeof(T).Name}>:{ex.Message}", ex: ex);
            }

            return this;
        }

        public override (string cmdParse, string cmd) ParseFormat(string command)
        {
            (string cmdParse, string cmd) v = base.ParseFormat(command);

            if (v.cmdParse == null && IsVariable) return ("", v.cmd);//返回""是为了能进到Parse里

            return v;
        }
    }
}
