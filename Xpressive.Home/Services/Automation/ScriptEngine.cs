using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xpressive.Home.Contracts.Automation;
using Xpressive.Home.Contracts.Messaging;
using Xpressive.Home.DatabaseModel;

namespace Xpressive.Home.Services.Automation
{
    internal class ScriptEngine : IScriptEngine
    {
        private readonly IList<IScriptObjectProvider> _scriptObjectProviders;
        private readonly IContextFactory _contextFactory;

        public ScriptEngine(IMessageQueue messageQueue, IEnumerable<IScriptObjectProvider> scriptObjectProviders, IContextFactory contextFactory)
        {
            _contextFactory = contextFactory;
            _scriptObjectProviders = scriptObjectProviders.ToList();

            messageQueue.Subscribe<ExecuteScriptMessage>(Notify);
        }

        public async Task ExecuteAsync(string scriptId, string triggerVariable, object triggerValue)
        {
            var script = await GetAsync(scriptId);
            Execute(script, triggerVariable, triggerValue, false);
        }

        public async Task ExecuteEvenIfDisabledAsync(string scriptId)
        {
            var script = await GetAsync(scriptId);
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

        public Task<Script> GetAsync(string id)
        {
            return _contextFactory.InScope(async context => await context.Script.FindAsync(id));
        }
    }
}
