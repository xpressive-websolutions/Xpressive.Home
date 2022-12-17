using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Xpressive.Home.Contracts.Messaging
{
    public sealed class CommandMessage : IMessageQueueMessage
    {
        public CommandMessage(string actionId, IDictionary<string, string> parameters)
        {
            ActionId = actionId;
            Parameters = new ReadOnlyDictionary<string, string>(parameters);
        }

        public CommandMessage(string gateway, string device, string action, IDictionary<string, string> parameters)
        {
            ActionId = $"{gateway}.{device}.{action}".Replace("..", ".");
            Parameters = new ReadOnlyDictionary<string, string>(parameters);
        }

        public string ActionId { get; }

        public IDictionary<string, string> Parameters { get; }
    }
}
