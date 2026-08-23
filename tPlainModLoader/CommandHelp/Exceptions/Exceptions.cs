using System;

namespace CommandHelp.Exceptions
{
    public class CommandException : Exception
    {
        public CommandException(int line = -1, string exceptionCommand = null, string exceptionmessage = null, Exception ex = null) : base(ex?.Message, ex)
        {
            _line = line;
            _exceptionCommand = exceptionCommand;
            _exceptionMessage = exceptionmessage;
        }


        protected int _line = -1;
        public int Line => _line;
        protected string _exceptionCommand = null;
        public string ExceptionCommand => _exceptionCommand;
        protected string _exceptionMessage = null;
        public string ExceptionMessage => _exceptionMessage;
    }

    /// <summary>
    /// 指令缺失
    /// </summary>
    public class CommandLackException : CommandException
    {
        public CommandLackException(int line = -1, string exceptionCommand = null, string exceptionmessage = null, Exception ex = null)
            : base(line, exceptionCommand, exceptionmessage, ex)
        {

        }
    }

    /// <summary>
    /// 指令解析失败
    /// </summary>
    public class CommandParseException : CommandException
    {
        public CommandParseException(int line = -1, string exceptionCommand = null, string exceptionmessage = null, Exception ex = null)
            : base(line, exceptionCommand, exceptionmessage, ex)
        {

        }
    }
}
