using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Serilog;
using Xpressive.Home.Contracts;
using Xpressive.Home.Contracts.Messaging;

namespace Xpressive.Home.Services.Messaging
{
    internal class MessageQueueLogListener : BackgroundService
    {
        public MessageQueueLogListener(IMessageQueue messageQueue)
        {
            messageQueue.Subscribe<UpdateVariableMessage>(Notify);
            messageQueue.Subscribe<CommandMessage>(Notify);
            messageQueue.Subscribe<NotifyUserMessage>(Notify);
            messageQueue.Subscribe<ExecuteScriptMessage>(Notify);
            messageQueue.Subscribe<NetworkDeviceFoundMessage>(Notify);
        }

        protected override Task ExecuteAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

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
