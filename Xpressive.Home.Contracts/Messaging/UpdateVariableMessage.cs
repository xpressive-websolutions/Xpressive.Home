namespace Xpressive.Home.Contracts.Messaging
{
    public sealed class UpdateVariableMessage : IMessageQueueMessage
    {
        private readonly string _name;
        private readonly object _value;
        private readonly string _unit;

        public UpdateVariableMessage(string name, object value, string unit = null)
        {
            _name = name;
            _value = value;
            _unit = unit;
        }

        public UpdateVariableMessage(string gateway, string device, string name, object value, string unit = null)
        {
            _name = $"{gateway}.{device}.{name}".Replace("..", ".");
            _value = value;
            _unit = unit;
        }

        public string Name => _name;
        public object Value => _value;
        public string Unit => _unit;
    }
}
