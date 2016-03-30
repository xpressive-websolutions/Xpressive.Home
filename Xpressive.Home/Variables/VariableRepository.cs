using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xpressive.Home.Contracts.Messaging;
using Xpressive.Home.Contracts.Variables;

namespace Xpressive.Home.Variables
{
    internal sealed class VariableRepository : IVariableRepository, IMessageQueueListener<UpdateVariableMessage>
    {
        private readonly IVariablePersistingService _variablePersistingService;
        private readonly object _variablesLock = new object();
        private readonly Dictionary<string, IVariable> _variables;

        public VariableRepository(IVariablePersistingService variablePersistingService)
        {
            _variablePersistingService = variablePersistingService;
            _variables = new Dictionary<string, IVariable>(StringComparer.Ordinal);
        }

        public T Get<T>(string name) where T : IVariable
        {
            lock (_variablesLock)
            {
                IVariable variable;
                if (_variables.TryGetValue(name, out variable))
                {
                    return (T)variable;
                }
                return default(T);
            }
        }

        public IEnumerable<IVariable> Get()
        {
            var result = new List<IVariable>();

            lock (_variablesLock)
            {
                result.AddRange(_variables.Values);
            }

            return result;
        }

        public void Notify(UpdateVariableMessage message)
        {
            lock (_variablesLock)
            {
                IVariable variable;
                if (_variables.TryGetValue(message.Name, out variable))
                {
                    variable.Value = message.Value;
                }
                else
                {
                    variable = CreateVariableByType(message.Value);
                    variable.Name = message.Name;
                    variable.Value = message.Value;
                    _variables.Add(message.Name, variable);
                }
            }
        }

        internal async Task InitAsync()
        {
            var variables = await _variablePersistingService.LoadAsync();

            lock (_variablesLock)
            {
                foreach (var variable in variables)
                {
                    _variables.Add(variable.Name, variable);
                }
            }
        }

        private IVariable CreateVariableByType(object value)
        {
            if (value is bool)
            {
                return new BooleanVariable();
            }

            if (value is string)
            {
                return new StringVariable();
            }

            if (value is double)
            {
                return new DoubleVariable();
            }

            throw new NotSupportedException(value.GetType().Name);
        }
    }
}
