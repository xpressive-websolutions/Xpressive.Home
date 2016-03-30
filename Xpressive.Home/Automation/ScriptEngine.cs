using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xpressive.Home.Contracts.Automation;
using Xpressive.Home.Contracts.Messaging;

namespace Xpressive.Home.Automation
{
    internal class ScriptEngine : IScriptEngine, IMessageQueueListener<UpdateVariableMessage>
    {
        private readonly IList<IScriptObjectProvider> _scriptObjectProviders;
        private readonly IScriptRepository _scriptRepository;
        private readonly IDictionary<string, IList<string>> _scriptsByVariableName;
        private readonly object _scriptsByVariableNameLock = new object();

        public ScriptEngine(IEnumerable<IScriptObjectProvider> scriptObjectProviders, IScriptRepository scriptRepository)
        {
            _scriptsByVariableName = new Dictionary<string, IList<string>>(StringComparer.Ordinal);
            _scriptRepository = scriptRepository;
            _scriptObjectProviders = scriptObjectProviders.ToList();
        }

        public void ExecuteWhenVariableChanges(string scriptId, string variable)
        {
            lock (_scriptsByVariableNameLock)
            {
                IList<string> scriptIds;
                if (!_scriptsByVariableName.TryGetValue(variable, out scriptIds))
                {
                    scriptIds = new List<string>();
                    _scriptsByVariableName.Add(variable, scriptIds);
                }

                if (!scriptIds.Contains(scriptId, StringComparer.Ordinal))
                {
                    scriptIds.Add(scriptId);
                }
            }
        }

        public async Task ExecuteAsync(string scriptId)
        {
            var script = await _scriptRepository.GetAsync(scriptId);
            Execute(script);
        }

        public async void Notify(UpdateVariableMessage message)
        {
            IList<string> scriptIds;

            lock (_scriptsByVariableNameLock)
            {
                if (!_scriptsByVariableName.TryGetValue(message.Name, out scriptIds))
                {
                    scriptIds = new List<string>(0);
                }
            }

            var scripts = await _scriptRepository.GetAsync(scriptIds);

            foreach (var script in scripts)
            {
                Execute(script);
            }
        }

        private void Execute(Script script)
        {
            if (script == null)
            {
                return;
            }

            var context = new ScriptExecutionContext(script, _scriptObjectProviders);
            context.Execute();
        }
    }
}
