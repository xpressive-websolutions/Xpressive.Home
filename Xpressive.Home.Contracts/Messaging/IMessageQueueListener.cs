namespace Xpressive.Home.Contracts.Messaging
{
    public interface IMessageQueueListener<in T> where T : IMessageQueueMessage
    {
        void Notify(T message);
    }
}
