using Serilog;
using Xpressive.Home.Contracts;
using Xpressive.Home.Contracts.Messaging;

namespace Xpressive.Home.Messaging
{
    internal class MessageQueueLogListener :
        IMessageQueueListener<UpdateVariableMessage>,
        IMessageQueueListener<CommandMessage>,
        IMessageQueueListener<NotifyUserMessage>,
        IMessageQueueListener<ExecuteScriptMessage>,
        IMessageQueueListener<NetworkDeviceFoundMessage>
    {
        public void Notify(UpdateVariableMessage message)
        {
            Log.Debug("{messageType} for variable {variableName} received.", message.GetType().Name, message.Name);
        }

        public void Notify(CommandMessage message)
        {
            Log.Information("{messageType} for action {actionId} received.", message.GetType().Name, message.ActionId);
        }

        public void Notify(NotifyUserMessage message)
        {
            Log.Information("{messageType} received: {notification}", message.GetType().Name, message.Notification);
        }

        public void Notify(ExecuteScriptMessage message)
        {
            Log.Information("{messageType} received for script {scriptId} with {delay}ms delay.", message.GetType().Name, message.ScriptId, message.DelayInMilliseconds);
        }

        public void Notify(NetworkDeviceFoundMessage message)
        {
            Log.Information("{messageType} received with {protocol}: IP={ipAddress} MAC={macAddress}", message.GetType().Name, message.Protocol, message.IpAddress, message.MacAddress.MacAddressToString());
        }
    }
}
