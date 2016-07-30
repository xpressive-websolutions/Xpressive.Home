namespace Xpressive.Home.Contracts.Variables
{
    public sealed class Int32Variable : IVariable
    {
        public Int32Variable() { }

        public Int32Variable(string name)
        {
            Name = name;
        }

        public string Name { get; set; }

        public int Value { get; set; }

        object IVariable.Value
        {
            get { return Value; }
            set { Value = (int)value; }
        }
    }
}
