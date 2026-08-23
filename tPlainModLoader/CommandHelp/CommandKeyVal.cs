using System.Collections.Generic;

namespace CommandHelp
{
    public class CommandKeyVal : CommandValue<object>
    {
        private object _val = null;


        public CommandKeyVal(string key, object val, bool isVariable = false) : base(isVariable, key)
        {
            _val = val;
        }


        public override object Run(ref int index, List<CommandObject> commandList)
        {
            return _val;
        }

        public override CommandObject Parse(string command)
        {
            if (command == "" && IsVariable)
            {
                IsDefault = true;
                _val = GetDefault();
                return this;
            }

            return Text == command ? this : null;
        }

        protected override object ArgConvertThrow(string arg)
        {
            throw new System.NotImplementedException();
        }

        protected override object GetDefault()
        {
            return null;
        }
    }
}
