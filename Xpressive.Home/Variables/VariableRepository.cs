using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Autofac;
using log4net;
using Xpressive.Home.Contracts.Messaging;
using Xpressive.Home.Contracts.Variables;

namespace Xpressive.Home.Variables
{
    internal sealed class VariableRepository : IVariableRepository, IMessageQueueListener<UpdateVariableMessage>, IStartable
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(VariableRepository));
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
                    try
                    {
                        variable.Value = message.Value;
                        variable.Unit = message.Unit;
                    }
                    catch (InvalidCastException)
                    {
                        _log.Error($"Unable to cast value {message.Value} of variable {message.Name}");
                    }
                }
                else
                {
                    variable = CreateVariableByType(message.Value);
                    variable.Name = message.Name;
                    variable.Value = message.Value;
                    variable.Unit = message.Unit;
                    _variables.Add(message.Name, variable);
                }

                _variablePersistingService.Save(variable);
            }
        }

        public void Start()
        {
            Task.WaitAll(InitAsync());
        }

        private async Task InitAsync()
        {
            try
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
            catch (Exception e)
            {
                _log.Error(e.Message, e);
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
