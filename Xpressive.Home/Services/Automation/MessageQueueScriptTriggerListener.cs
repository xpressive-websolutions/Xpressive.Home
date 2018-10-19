using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Serilog;
using Xpressive.Home.Contracts.Automation;
using Xpressive.Home.Contracts.Messaging;
using Xpressive.Home.Contracts.Variables;

namespace Xpressive.Home.Services.Automation
{
    internal class MessageQueueScriptTriggerListener : BackgroundService
    {
        private readonly IVariableRepository _variableRepository;
        private readonly IScriptTriggerService _scriptTriggerService;
        private readonly IScriptEngine _scriptEngine;
        private readonly BlockingCollection<Tuple<string, object>> _variables;
        private readonly SingleTaskRunner _taskRunner;
        private Dictionary<string, object> _variableValues;

        public MessageQueueScriptTriggerListener(IMessageQueue messageQueue, IVariableRepository variableRepository, IScriptTriggerService scriptTriggerService, IScriptEngine scriptEngine)
        {
            _variableRepository = variableRepository;
            _scriptTriggerService = scriptTriggerService;
            _scriptEngine = scriptEngine;

            messageQueue.Subscribe<UpdateVariableMessage>(Notify);

            _variables = new BlockingCollection<Tuple<string, object>>();
            _taskRunner = new SingleTaskRunner();
            _variableValues = new Dictionary<string, object>(StringComparer.Ordinal);
        }

        public void Notify(UpdateVariableMessage message)
        {
            _variables.Add(Tuple.Create(message.Name, message.Value));
            _taskRunner.StartIfNotAlreadyRunning(HandleVariableUpdatesAsync);
        }

        protected override Task ExecuteAsync(CancellationToken cancellationToken)
        {
            _variableValues = _variableRepository
                .Get()
                .ToDictionary(v => v.Name, v => v.Value, StringComparer.Ordinal);
            return Task.CompletedTask;
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
                    Log.Debug("Execute script with id {scriptId}", script.ScriptId);
                    await _scriptEngine.ExecuteAsync(script.ScriptId, variable, value);
                }
            });
        }
    }
}
