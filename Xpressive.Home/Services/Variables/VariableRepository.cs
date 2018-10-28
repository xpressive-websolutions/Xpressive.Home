using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Serilog;
using Xpressive.Home.Contracts.Messaging;
using Xpressive.Home.Contracts.Variables;

namespace Xpressive.Home.Services.Variables
{
    internal sealed class VariableRepository : BackgroundService, IVariableRepository
    {
        private readonly IVariablePersistingService _variablePersistingService;
        private readonly object _variablesLock = new object();
        private readonly Dictionary<string, IVariable> _variables;

        public VariableRepository(IVariablePersistingService variablePersistingService, IMessageQueue messageQueue)
        {
            _variablePersistingService = variablePersistingService;
            _variables = new Dictionary<string, IVariable>(StringComparer.Ordinal);

            messageQueue.Subscribe<UpdateVariableMessage>(Notify);
        }

        public T Get<T>(string name) where T : IVariable
        {
            lock (_variablesLock)
            {
                if (_variables.TryGetValue(name, out IVariable variable))
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
                if (_variables.TryGetValue(message.Name, out IVariable variable))
                {
                    try
                    {
                        variable.Value = message.Value;
                        variable.Unit = message.Unit;
                    }
                    catch (InvalidCastException)
                    {
                        Log.Error("Unable to cast value {messageValue} of variable {messageName}", message.Value, message.Name);
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

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
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
                Log.Error(e, e.Message);
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
