namespace Xpressive.Home.Contracts.Messaging
{
    public sealed class NotifyUserMessage : IMessageQueueMessage
    {
        private readonly string _notification;

        public NotifyUserMessage(string notification)
        {
            _notification = notification;
        }

        public string Notification => _notification;
    }
}
