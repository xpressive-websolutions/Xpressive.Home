using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Quartz;
using Quartz.Impl;
using Quartz.Spi;
using Xpressive.Home.Contracts.Automation;

namespace Xpressive.Home.Automation
{
    internal class CronService : ICronService
    {
        private static readonly object _schedulerLock = new object();
        private static volatile IScheduler _scheduler;
        private readonly IJobFactory _jobFactory;
        private readonly IScheduledScriptRepository _scheduledScriptRepository;

        public CronService(IJobFactory jobFactory, IScheduledScriptRepository scheduledScriptRepository)
        {
            _jobFactory = jobFactory;
            _scheduledScriptRepository = scheduledScriptRepository;
        }

        public async Task<ScheduledScript> ScheduleAsync(string scriptId, string cronTab)
        {
            var id = Guid.NewGuid().ToString("n");

            Schedule(id, cronTab);

            await _scheduledScriptRepository.InsertAsync(id, scriptId, cronTab);

            return new ScheduledScript
            {
                Id = id,
                ScriptId = scriptId,
                CronTab = cronTab
            };
        }

        public async Task DeleteScheduleAsync(string id)
        {
            _scheduler.DeleteJob(new JobKey(id));
            await _scheduledScriptRepository.DeleteAsync(id);
        }

        public async Task<IEnumerable<ScheduledScript>> GetSchedulesAsync()
        {
            return await _scheduledScriptRepository.GetAsync();
        }

        internal async Task InitAsync()
        {
            if (_scheduler != null)
            {
                return;
            }

            lock (_schedulerLock)
            {
                if (_scheduler != null)
                {
                    return;
                }

                var factory = new StdSchedulerFactory();
                _scheduler = factory.GetScheduler();
                _scheduler.JobFactory = _jobFactory;
                _scheduler.Start();
            }

            await SchedulePersistedJobsAsync();
        }

        private void Schedule(string id, string cronTab)
        {
            var job = JobBuilder.Create<RecurrentScriptExecution>()
                .WithIdentity(id)
                .Build();

            var trigger = TriggerBuilder.Create()
                .WithIdentity(id)
                .WithCronSchedule(cronTab)
                .Build();

            _scheduler.ScheduleJob(job, trigger);
        }

        private async Task SchedulePersistedJobsAsync()
        {
            var schedules = await _scheduledScriptRepository.GetAsync();

            foreach (var schedule in schedules)
            {
                Schedule(schedule.Id, schedule.CronTab);
            }
        }
    }
}
