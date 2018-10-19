﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Serilog;
using Xpressive.Home.Contracts.Messaging;

namespace Xpressive.Home.Services.Messaging
{
    internal sealed class MessageQueue : IMessageQueue
    {
        private static readonly object _lock = new object();
        private readonly Dictionary<Type, List<Action<object>>> _subscriptions = new Dictionary<Type, List<Action<object>>>();

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
                    Log.Error(e, e.Message);
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
