namespace Xpressive.Home.Contracts.Variables
{
    public sealed class BooleanVariable : IVariable
    {
        public BooleanVariable() { }

        public BooleanVariable(string name)
        {
            Name = name;
        }

        public string Name { get; set; }

        public bool Value { get; set; }

        public string Unit { get; set; }

        object IVariable.Value
        {
            get { return Value; }
            set { Value = (bool)value; }
        }
    }
}
