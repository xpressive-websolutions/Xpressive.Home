using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xpressive.Home.Contracts.Variables;
using Xpressive.Home.DatabaseModel;

namespace Xpressive.Home.Services.Variables
{
    internal sealed class VariablePersistingService : IVariablePersistingService
    {
        private static readonly BlockingCollection<IVariable> _variablesToSave = new BlockingCollection<IVariable>();
        private static readonly SingleTaskRunner _taskRunner = new SingleTaskRunner();
        private static readonly HashSet<string> _persistedVariables = new HashSet<string>(StringComparer.Ordinal);
        private readonly IContextFactory _contextFactory;

        public VariablePersistingService(IContextFactory contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public void Save(IVariable variable)
        {
            _variablesToSave.Add(variable);
            _taskRunner.StartIfNotAlreadyRunning(SaveVariables);
        }

        public Task<IEnumerable<IVariable>> LoadAsync()
        {
            return _contextFactory.InScope(async context =>
            {
                var variables = await context.Variable.ToListAsync();

                foreach (var variable in variables)
                {
                    if (!_persistedVariables.Contains(variable.Name))
                    {
                        _persistedVariables.Add(variable.Name);
                    }
                }

                return variables.Select(CreateVariable);
            });
        }

        private Task SaveVariables()
        {
            return _contextFactory.InScope(async context =>
            {
                while (_variablesToSave.Count > 0)
                {
                    var variable = _variablesToSave.Take();

                    var persistedVariable = new PersistedVariable
                    {
                        Name = variable.Name,
                        DataType = variable.Value.GetType().Name,
                        Value = variable.Value.ToString()
                    };

                    if (_persistedVariables.Contains(variable.Name))
                    {
                        var existing = await context.Variable.FindAsync(variable.Name);
                        existing.Value = persistedVariable.Value;
                    }
                    else
                    {
                        context.Variable.Add(persistedVariable);
                        _persistedVariables.Add(variable.Name);
                    }

                    await context.SaveChangesAsync();
                }
            });
        }

        private IVariable CreateVariable(PersistedVariable persistedVariable)
        {
            var variable = CreateVariableByType(persistedVariable.DataType);
            variable.Name = persistedVariable.Name;
            variable.Value = ConvertVariableValue(persistedVariable.Value, persistedVariable.DataType);
            return variable;
        }

        private object ConvertVariableValue(string value, string dataType)
        {
            switch (dataType.ToLowerInvariant())
            {
                case "boolean": return bool.Parse(value);
                case "double": return double.Parse(value);
                case "string": return value;
                case "binary": return value;
            }

            throw new NotSupportedException(dataType);
        }

        private IVariable CreateVariableByType(string dataType)
        {
            switch (dataType.ToLowerInvariant())
            {
                case "boolean": return new BooleanVariable();
                case "double": return new DoubleVariable();
                case "string": return new StringVariable();
                case "binary": return new BinaryVariable();
            }

            throw new NotSupportedException(dataType);
        }
    }

    public class PersistedVariable
    {
        public string Name { get; set; }
        public string DataType { get; set; }
        public string Value { get; set; }
    }
}
