namespace Xpressive.Home.Contracts.Variables
{
    public sealed class DoubleVariable : IVariable
    {
        public DoubleVariable() { }

        public DoubleVariable(string name)
        {
            Name = name;
        }

        public string Name { get; set; }

        public double Value { get; set; }

        public string Unit { get; set; }

        object IVariable.Value
        {
            get { return Value; }
            set { Value = (double)value; }
        }
    }
}
