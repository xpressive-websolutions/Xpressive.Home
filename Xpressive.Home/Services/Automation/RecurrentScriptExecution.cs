using System.Collections.Generic;
using System.Threading.Tasks;
using Quartz;
using Xpressive.Home.Contracts.Automation;

namespace Xpressive.Home.Services.Automation
{
    internal class RecurrentScriptExecution : IJob
    {
        private readonly Script _script;
        private readonly IList<IScriptObjectProvider> _scriptObjectProviders;

        public RecurrentScriptExecution(Script script, IList<IScriptObjectProvider> scriptObjectProviders)
        {
            _script = script;
            _scriptObjectProviders = scriptObjectProviders;
        }

        public Task Execute(IJobExecutionContext context)
        {
            var scriptContext = new ScriptExecutionContext(_script, _scriptObjectProviders);
            scriptContext.Execute("scheduler", context.ScheduledFireTimeUtc?.DateTime.ToString("s"));
            return Task.CompletedTask;
        }
    }
}
