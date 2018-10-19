using System;

namespace Xpressive.Home.Contracts.Messaging
{
    [Obsolete]
    public interface IMessageQueueListener<in T> where T : IMessageQueueMessage
    {
        void Notify(T message);
    }
}
