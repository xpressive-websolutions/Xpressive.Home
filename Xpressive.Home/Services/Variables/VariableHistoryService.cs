using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Xpressive.Home.Contracts.Messaging;
using Xpressive.Home.Contracts.Variables;

namespace Xpressive.Home.Services.Variables
{
    internal sealed class VariableHistoryService : IVariableHistoryService
    {
        private readonly ConcurrentDictionary<string, LimitedVariableBuffer> _buffers;
        private readonly double _limitInHours;

        public VariableHistoryService(IMessageQueue messageQueue)
        {
            _limitInHours = 24; // TODO
            _buffers = new ConcurrentDictionary<string, LimitedVariableBuffer>(StringComparer.OrdinalIgnoreCase);

            messageQueue.Subscribe<UpdateVariableMessage>(Notify);
        }

        public IEnumerable<IVariableHistoryValue> Get(string name)
        {
            var buffer = _buffers.GetOrAdd(name, _ => new LimitedVariableBuffer(_limitInHours));
            return buffer.Get().Select(t => new VariableHistoryValue(t.Item1, t.Item2));
        }

        public void Notify(UpdateVariableMessage message)
        {
            var buffer = _buffers.GetOrAdd(message.Name, _ => new LimitedVariableBuffer(_limitInHours));
            buffer.Add(message.Value);
        }

        private class VariableHistoryValue : IVariableHistoryValue
        {
            public VariableHistoryValue(DateTime effectiveDate, object value)
            {
                EffectiveDate = effectiveDate;
                Value = value;
            }

            public DateTime EffectiveDate { get; }
            public object Value { get; }
        }
    }
}
