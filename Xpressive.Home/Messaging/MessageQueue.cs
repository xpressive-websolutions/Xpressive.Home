using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xpressive.Home.Contracts.Messaging;

namespace Xpressive.Home.Messaging
{
    internal sealed class MessageQueue : IMessageQueue
    {
        private static readonly object _lock = new object();
        private readonly Dictionary<Type, List<Action<object>>> _subscriptions = new Dictionary<Type, List<Action<object>>>();

        public MessageQueue(
            IList<IMessageQueueListener<UpdateVariableMessage>> updateVariableListeners,
            IList<IMessageQueueListener<NotifyUserMessage>> notifyUserListeners,
            IList<IMessageQueueListener<CommandMessage>> commandListeners)
        {
            foreach (var listener in updateVariableListeners)
            {
                Subscribe<UpdateVariableMessage>(listener.Notify);
            }

            foreach (var listener in notifyUserListeners)
            {
                Subscribe<NotifyUserMessage>(listener.Notify);
            }

            foreach (var listener in commandListeners)
            {
                Subscribe<CommandMessage>(listener.Notify);
            }
        }

        public void Publish<T>(T message) where T : IMessageQueueMessage
        {
            var t = typeof(T);
            List<Action<object>> subscriber;

            lock (_lock)
            {
                if (!_subscriptions.TryGetValue(t, out subscriber))
                {
                    return;
                }

                subscriber = subscriber.ToList();
            }

            Parallel.ForEach(subscriber, action =>
            {
                try
                {
                    action(message);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            });
        }

        public void Subscribe<T>(Action<T> action) where T : IMessageQueueMessage
        {
            var t = typeof(T);

            lock (_lock)
            {
                List<Action<object>> subscriber;
                if (!_subscriptions.TryGetValue(t, out subscriber))
                {
                    subscriber = new List<Action<object>>();
                    _subscriptions.Add(t, subscriber);
                }

                subscriber.Add(x => action((T)x));
            }
        }
    }
}
