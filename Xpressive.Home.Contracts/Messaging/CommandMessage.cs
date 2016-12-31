using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Xpressive.Home.Contracts.Messaging
{
    public sealed class CommandMessage : IMessageQueueMessage
    {
        private readonly string _actionId;
        private readonly IDictionary<string, string> _parameters;

        public CommandMessage(string actionId, IDictionary<string, string> parameters)
        {
            _actionId = actionId;
            _parameters = new ReadOnlyDictionary<string, string>(parameters);
        }

        public CommandMessage(string gateway, string device, string action, IDictionary<string, string> parameters)
        {
            _actionId = $"{gateway}.{device}.{action}".Replace("..", ".");
            _parameters = new ReadOnlyDictionary<string, string>(parameters);
        }

        public string ActionId => _actionId;
        public IDictionary<string, string> Parameters => _parameters;
    }
}
