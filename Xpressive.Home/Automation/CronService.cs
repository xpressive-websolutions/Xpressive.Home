using System;
using System.Threading.Tasks;
using Autofac;
using Quartz;
using Quartz.Impl;
using Quartz.Spi;
using Xpressive.Home.Contracts.Automation;

namespace Xpressive.Home.Automation
{
    internal class CronService : IStartable, ICronService
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

        public async Task<ScheduledScript> ScheduleAsync(Guid scriptId, string cronTab)
        {
            var id = Guid.NewGuid();

            await _scheduledScriptRepository.InsertAsync(id, scriptId, cronTab);

            Schedule(id, cronTab);

            return new ScheduledScript
            {
                Id = id,
                ScriptId = scriptId,
                CronTab = cronTab
            };
        }

        public async Task DeleteScheduleAsync(Guid id)
        {
            _scheduler.DeleteJob(new JobKey(id.ToString("n")));
            await _scheduledScriptRepository.DeleteAsync(id);
        }

        public void Start()
        {
            Task.Run(InitAsync);
        }

        private async Task InitAsync()
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

        private void Schedule(Guid id, string cronTab)
        {
            var job = JobBuilder.Create<RecurrentScriptExecution>()
                .WithIdentity(id.ToString("n"))
                .Build();

            var trigger = TriggerBuilder.Create()
                .WithIdentity(id.ToString("n"))
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
