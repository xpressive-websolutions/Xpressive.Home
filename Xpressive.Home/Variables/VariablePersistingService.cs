using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using NPoco;
using Xpressive.Home.Contracts.Variables;

namespace Xpressive.Home.Variables
{
    internal sealed class VariablePersistingService : IVariablePersistingService
    {
        private static readonly BlockingCollection<IVariable> _variablesToSave = new BlockingCollection<IVariable>();
        private static readonly SingleTaskRunner _taskRunner = new SingleTaskRunner();
        private static readonly HashSet<string> _persistedVariables = new HashSet<string>(StringComparer.Ordinal);
        private readonly bool _isInMemory;

        public VariablePersistingService()
        {
            _isInMemory = true;

            foreach (ConnectionStringSettings cs in ConfigurationManager.ConnectionStrings)
            {
                if (cs.Name.Equals("ConnectionString"))
                {
                    _isInMemory = false;
                    break;
                }
            }
        }

        public void Save(IVariable variable)
        {
            _variablesToSave.Add(variable);
            _taskRunner.StartIfNotAlreadyRunning(SaveVariables);
        }

        public async Task<IEnumerable<IVariable>> LoadAsync()
        {
            if (_isInMemory)
            {
                return new List<IVariable>(0);
            }

            List<PersistedVariable> persistedVariables;

            using (var database = new Database("ConnectionString"))
            {
                var sql = "select * from Variable";
                persistedVariables = await database.FetchAsync<PersistedVariable>(sql);
            }

            foreach (var variable in persistedVariables)
            {
                if (!_persistedVariables.Contains(variable.Name))
                {
                    _persistedVariables.Add(variable.Name);
                }
            }

            return persistedVariables.Select(CreateVariable);
        }

        private async Task SaveVariables()
        {
            if (_isInMemory)
            {
                return;
            }

            using (var database = new Database("ConnectionString"))
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
                        await database.UpdateAsync("Variable", "Name", persistedVariable, variable.Name, new[] {"Value"});
                    }
                    else
                    {
                        await database.InsertAsync("Variable", "Name", false, persistedVariable);
                        _persistedVariables.Add(variable.Name);
                    }
                }
            }
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
            }

            throw new NotSupportedException(dataType);
        }

        private class PersistedVariable
        {
            public string Name { get; set; }
            public string DataType { get; set; }
            public string Value { get; set; }
        }
    }
}
