namespace CommandHelp
{
    public class CommandFalse : CommandKeyVal
    {
        public CommandFalse() : base("false", false) { }
    }

    public class CommandTrue : CommandKeyVal
    {
        public CommandTrue() : base("true", true) { }
    }

    public class CommandInt : CommandValue<int>
    {
        public override string Text => "<int>";

        protected override int ArgConvertThrow(string arg) => int.Parse(arg);

        protected override int GetDefault() => default;
    }

    public class CommandFloat : CommandValue<float>
    {
        public override string Text => "<float>";

        protected override float ArgConvertThrow(string arg) => float.Parse(arg);

        protected override float GetDefault() => default;
    }
}
