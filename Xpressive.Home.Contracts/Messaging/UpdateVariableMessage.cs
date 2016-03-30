namespace Xpressive.Home.Contracts.Messaging
{
    public sealed class UpdateVariableMessage : IMessageQueueMessage
    {
        private readonly string _name;
        private readonly object _value;

        public UpdateVariableMessage(string name, object value)
        {
            _name = name;
            _value = value;
        }

        public UpdateVariableMessage(string gateway, string device, string name, object value)
        {
            _name = $"{gateway}.{device}.{name}".Replace("..", ".");
            _value = value;
        }

        public string Name => _name;
        public object Value => _value;
    }
}