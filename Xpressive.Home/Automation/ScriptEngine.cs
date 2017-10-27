using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xpressive.Home.Contracts.Automation;
using Xpressive.Home.Contracts.Messaging;

namespace Xpressive.Home.Automation
{
    internal class ScriptEngine : IScriptEngine, IMessageQueueListener<ExecuteScriptMessage>
    {
        private readonly IList<IScriptObjectProvider> _scriptObjectProviders;
        private readonly IScriptRepository _scriptRepository;

        public ScriptEngine(IEnumerable<IScriptObjectProvider> scriptObjectProviders, IScriptRepository scriptRepository)
        {
            _scriptRepository = scriptRepository;
            _scriptObjectProviders = scriptObjectProviders.ToList();
        }

        public async Task ExecuteAsync(Guid scriptId, string triggerVariable, object triggerValue)
        {
            var script = await _scriptRepository.GetAsync(scriptId);
            Execute(script, triggerVariable, triggerValue, false);
        }

        public async Task ExecuteEvenIfDisabledAsync(Guid scriptId)
        {
            var script = await _scriptRepository.GetAsync(scriptId);
            Execute(script, null, null, true);
        }

        public void Notify(ExecuteScriptMessage message)
        {
            Task.Factory.StartNew(async () =>
            {
                if (message.DelayInMilliseconds > 0)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(message.DelayInMilliseconds));
                }

                await ExecuteAsync(message.ScriptId, null, null);
            }, TaskCreationOptions.DenyChildAttach);
        }

        private void Execute(Script script, string triggerVariable, object triggerValue, bool evenIfDisabled)
        {
            if (script == null)
            {
                return;
            }

            var context = new ScriptExecutionContext(script, _scriptObjectProviders);

            if (evenIfDisabled)
            {
                context.ExecuteEvenIfDisabled(triggerVariable, triggerValue);
            }
            else
            {
                context.Execute(triggerVariable, triggerValue);
            }
        }
    }
}
