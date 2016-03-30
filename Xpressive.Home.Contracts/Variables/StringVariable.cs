namespace Xpressive.Home.Contracts.Variables
{
    public sealed class StringVariable : IVariable
    {
        public StringVariable() { }

        public StringVariable(string name)
        {
            Name = name;
        }

        public string Name { get; set; }

        public string Value { get; set; }

        object IVariable.Value
        {
            get { return Value; }
            set { Value = (string)value; }
        }
    }
}