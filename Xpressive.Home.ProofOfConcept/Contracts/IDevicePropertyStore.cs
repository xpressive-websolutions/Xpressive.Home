using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Xpressive.Home.ProofOfConcept.Contracts
{
    public interface IVariableRepository
    {
        void Register<T>(T variable) where T : IVariable;

        T Get<T>(string name) where T : IVariable;
    }

    public interface IVariable
    {
        string Name { get; set; }

        object Value { get; set; }
    }

    public class BooleanVariable : IVariable
    {
        public BooleanVariable() { }

        public BooleanVariable(string name)
        {
            Name = name;
        }

        public string Name { get; set; }

        public bool Value { get; set; }

        object IVariable.Value
        {
            get { return Value; }
            set { Value = (bool)value; }
        }
    }

    public class DoubleVariable : IVariable
    {
        public DoubleVariable() { }

        public DoubleVariable(string name)
        {
            Name = name;
        }

        public string Name { get; set; }

        public double Value { get; set; }

        object IVariable.Value
        {
            get { return Value; }
            set { Value = (double)value; }
        }
    }

    public class StringVariable : IVariable
    {
        public StringVariable() { }

        public StringVariable(string name)
        {
            Name = name;
        }

        public string Name { get; set; }

        public string Value { get; set; }

        object IVariable.Value
        {
            get { return Value; }
            set { Value = (string)value; }
        }
    }

    internal sealed class VariableRepository : IVariableRepository
    {
        private readonly object _variablesLock = new object();
        private readonly Dictionary<string, IVariable> _variables;

        public VariableRepository(IMessageQueue messageQueue)
        {
            messageQueue.Subscribe((UpdateVariableMessage msg) => HandleMessage(msg));
        }

        public T Get<T>(string name) where T : IVariable
        {
            IVariable variable;
            if (_variables.TryGetValue(name, out variable))
            {
                return (T)variable;
            }
            return default(T);
        }

        public void Register<T>(T variable) where T : IVariable
        {
            lock (_variablesLock)
            {
                if (_variables.ContainsKey(variable.Name))
                {
                    throw new InvalidOperationException("Variable already registered.");
                }

                _variables.Add(variable.Name, variable);
            }

            // TODO: load current value from database
        }

        private void HandleMessage(UpdateVariableMessage message)
        {
            IVariable variable;
            if (_variables.TryGetValue(message.Name, out variable))
            {
                variable.Value = message.Value;
            }
        }
    }

    public interface IMessageQueueMessage { }

    public sealed class CommandMessage : IMessageQueueMessage
    {
        private readonly string _actionId;
        private readonly IDictionary<string, string> _parameters;

        public CommandMessage(string actionId, IDictionary<string, string> parameters)
        {
            _actionId = actionId;
            _parameters = new ReadOnlyDictionary<string, string>(parameters);
        }

        public CommandMessage(string gateway, string device, string action, IDictionary<string, string> parameters)
        {
            _actionId = $"{gateway}.{device}.{action}".Replace("..", ".");
            _parameters = new ReadOnlyDictionary<string, string>(parameters);
        }

        public string ActionId => _actionId;
        public IDictionary<string, string> Parameters => _parameters;
    }

    public sealed class UpdateVariableMessage : IMessageQueueMessage
    {
        private readonly string _name;
        private readonly object _value;

        public UpdateVariableMessage(string name, object value)
        {
            _name = name;
            _value = value;
        }

        public UpdateVariableMessage(string gateway, string device, string name, object value)
        {
            _name = $"{gateway}.{device}.{name}".Replace("..", ".");
            _value = value;
        }

        public string Name => _name;
        public object Value => _value;
    }

    public sealed class UserNotificationMessage : IMessageQueueMessage
    {
        private readonly string _notification;

        public UserNotificationMessage(string notification)
        {
            _notification = notification;
        }

        public string Notification => _notification;
    }

    public interface IMessageQueueListener<T> where T : IMessageQueueMessage
    {
        void Notify(T message);
    }

    public interface IMessageQueue
    {
        void Publish<T>(T message) where T : IMessageQueueMessage;

        void Subscribe<T>(Action<T> action) where T : IMessageQueueMessage;
    }

    internal sealed class MessageQueue : IMessageQueue
    {
        private readonly object _lock = new object();
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
            List<Action<object>> subscriber;

            lock (_lock)
            {
                if (!_subscriptions.TryGetValue(t, out subscriber))
                {
                    subscriber = new List<Action<object>>();
                    _subscriptions.Add(t, subscriber);
                }

                subscriber.Add((object x) => action((T)x));
            }
        }
    }
}