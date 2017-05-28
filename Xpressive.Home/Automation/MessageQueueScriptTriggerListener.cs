using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using log4net;
using Xpressive.Home.Contracts.Automation;
using Xpressive.Home.Contracts.Messaging;
using Xpressive.Home.Contracts.Variables;

namespace Xpressive.Home.Automation
{
    internal class MessageQueueScriptTriggerListener : IMessageQueueListener<UpdateVariableMessage>, IStartable
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(MessageQueueScriptTriggerListener));
        private readonly IVariableRepository _variableRepository;
        private readonly IScriptTriggerService _scriptTriggerService;
        private readonly IScriptEngine _scriptEngine;
        private readonly BlockingCollection<Tuple<string, object>> _variables;
        private readonly SingleTaskRunner _taskRunner;
        private Dictionary<string, object> _variableValues;

        public MessageQueueScriptTriggerListener(IVariableRepository variableRepository, IScriptTriggerService scriptTriggerService, IScriptEngine scriptEngine)
        {
            _variableRepository = variableRepository;
            _scriptTriggerService = scriptTriggerService;
            _scriptEngine = scriptEngine;

            _variables = new BlockingCollection<Tuple<string, object>>();
            _taskRunner = new SingleTaskRunner();
            _variableValues = new Dictionary<string, object>(StringComparer.Ordinal);
        }

        public void Notify(UpdateVariableMessage message)
        {
            _variables.Add(Tuple.Create(message.Name, message.Value));
            _taskRunner.StartIfNotAlreadyRunning(HandleVariableUpdatesAsync);
        }

        public void Start()
        {
            _variableValues = _variableRepository
                .Get()
                .ToDictionary(v => v.Name, v => v.Value, StringComparer.Ordinal);
        }

        private async Task HandleVariableUpdatesAsync()
        {
            Tuple<string, object> update;
            while (_variables.TryTake(out update))
            {
                await HandleVariableUpdateAsync(update.Item1, update.Item2);
            }
        }

        private async Task HandleVariableUpdateAsync(string variable, object value)
        {
            object currentValue;
            if (_variableValues.TryGetValue(variable, out currentValue) && Equals(currentValue, value))
            {
                return;
            }

            _variableValues[variable] = value;

            await _scriptTriggerService.GetTriggersByVariableAsync(variable).ContinueWith(async result =>
            {
                foreach (var script in result.Result)
                {
                    _log.Debug($"Execute script with id {script.ScriptId}");
                    await _scriptEngine.ExecuteAsync(script.ScriptId, variable, value);
                }
            });
        }
    }
}
