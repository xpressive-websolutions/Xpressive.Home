using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xpressive.Home.Contracts.Automation;

namespace Xpressive.Home.Automation
{
    internal class ScriptEngine : IScriptEngine
    {
        private readonly IList<IScriptObjectProvider> _scriptObjectProviders;
        private readonly IScriptRepository _scriptRepository;

        public ScriptEngine(IEnumerable<IScriptObjectProvider> scriptObjectProviders, IScriptRepository scriptRepository)
        {
            _scriptRepository = scriptRepository;
            _scriptObjectProviders = scriptObjectProviders.ToList();
        }

        public async Task ExecuteAsync(Guid scriptId)
        {
            var script = await _scriptRepository.GetAsync(scriptId);
            Execute(script);
        }

        public async Task ExecuteEvenIfDisabledAsync(Guid scriptId)
        {
            var script = await _scriptRepository.GetAsync(scriptId);
            Execute(script, true);
        }

        private void Execute(Script script, bool evenIfDisabled = false)
        {
            if (script == null)
            {
                return;
            }

            var context = new ScriptExecutionContext(script, _scriptObjectProviders);

            if (evenIfDisabled)
            {
                context.ExecuteEvenIfDisabled();
            }
            else
            {
                context.Execute();
            }
        }
    }
}
