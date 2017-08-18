using log4net;
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
        private static readonly ILog _log = LogManager.GetLogger(typeof(MessageQueueLogListener));

        public void Notify(UpdateVariableMessage message)
        {
            _log.Debug($"{message.GetType().Name} for variable {message.Name} received.");
        }

        public void Notify(CommandMessage message)
        {
            _log.Info($"{message.GetType().Name} for action {message.ActionId} received.");
        }

        public void Notify(NotifyUserMessage message)
        {
            _log.Info($"{message.GetType().Name} received: {message.Notification}");
        }

        public void Notify(ExecuteScriptMessage message)
        {
            _log.Info($"{message.GetType().Name} received for script {message.ScriptId} with {message.DelayInMilliseconds}ms delay.");
        }

        public void Notify(NetworkDeviceFoundMessage message)
        {
            _log.Info($"{message.GetType().Name} received with {message.Protocol}: IP={message.IpAddress} MAC={message.MacAddress.MacAddressToString()}");
        }
    }
}
