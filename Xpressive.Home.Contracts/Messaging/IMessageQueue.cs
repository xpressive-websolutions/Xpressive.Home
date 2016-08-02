using System;

namespace Xpressive.Home.Contracts.Messaging
{
    public interface IMessageQueue
    {
        void Publish<T>(T message) where T : IMessageQueueMessage;

        void Subscribe<T>(Action<T> action) where T : IMessageQueueMessage;
    }
}
