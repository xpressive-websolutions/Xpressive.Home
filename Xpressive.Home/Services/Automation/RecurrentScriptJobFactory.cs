using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Quartz;
using Quartz.Spi;
using Xpressive.Home.Contracts.Automation;
using Xpressive.Home.DatabaseModel;

namespace Xpressive.Home.Services.Automation
{
    internal class RecurrentScriptJobFactory : IJobFactory
    {
        private readonly IScheduledScriptRepository _repository;
        private readonly IContextFactory _contextFactory;
        private readonly IList<IScriptObjectProvider> _scriptObjectProviders;

        public RecurrentScriptJobFactory(IScheduledScriptRepository repository, IEnumerable<IScriptObjectProvider> scriptObjectProviders, IContextFactory contextFactory)
        {
            _repository = repository;
            _contextFactory = contextFactory;
            _scriptObjectProviders = scriptObjectProviders.ToList();
        }

        public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler)
        {
            var task = FindJobAsync(bundle.JobDetail.Key.Name);
            Task.WaitAll(task);
            return task.Result ?? new DoNothingJob();
        }

        public void ReturnJob(IJob job) { }

        private async Task<IJob> FindJobAsync(string id)
        {
            var scheduledScript = await _repository.GetAsync(id);

            if (scheduledScript == null)
            {
                return null;
            }

            var script = await GetAsync(scheduledScript.ScriptId);

            if (script == null)
            {
                return null;
            }

            return new RecurrentScriptExecution(script, _scriptObjectProviders);
        }

        public Task<Script> GetAsync(string id)
        {
            return _contextFactory.InScope(async context => await context.Script.FindAsync(id));
        }
    }
}
