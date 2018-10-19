using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Quartz;
using Quartz.Spi;
using Xpressive.Home.Contracts.Automation;

namespace Xpressive.Home.Services.Automation
{
    internal class RecurrentScriptJobFactory : IJobFactory
    {
        private readonly IScheduledScriptRepository _repository;
        private readonly IScriptRepository _scriptRepository;
        private readonly IList<IScriptObjectProvider> _scriptObjectProviders;

        public RecurrentScriptJobFactory(IScheduledScriptRepository repository, IEnumerable<IScriptObjectProvider> scriptObjectProviders, IScriptRepository scriptRepository)
        {
            _repository = repository;
            _scriptObjectProviders = scriptObjectProviders.ToList();
            _scriptRepository = scriptRepository;
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
            Guid guid;
            if (!Guid.TryParse(id, out guid))
            {
                return null;
            }

            var scheduledScript = await _repository.GetAsync(guid);

            if (scheduledScript == null)
            {
                return null;
            }

            var script = await _scriptRepository.GetAsync(scheduledScript.ScriptId);

            if (script == null)
            {
                return null;
            }

            return new RecurrentScriptExecution(script, _scriptObjectProviders);
        }
    }
}
