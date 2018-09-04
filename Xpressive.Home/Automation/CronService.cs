using Autofac;
using Quartz;
using Quartz.Impl;
using Quartz.Spi;
using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xpressive.Home.Contracts.Automation;

namespace Xpressive.Home.Automation
{
    internal class CronService : IStartable, ICronService, IDisposable
    {
        private static readonly SemaphoreSlim _schedulerLock = new SemaphoreSlim(1, 1);
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
            if (!CronExpression.IsValidExpression(cronTab))
            {
                throw new InvalidOperationException($"Cron tab {cronTab} is invalid.");
            }

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
            await _scheduler.DeleteJob(new JobKey(id.ToString("n")));
            await _scheduledScriptRepository.DeleteAsync(id);
        }

        public void Start()
        {
            Task.Run(InitAsync);
        }

        public void Dispose()
        {
            _scheduler.Shutdown(false);
        }

        private async Task InitAsync()
        {
            try
            {
                if (_scheduler != null)
                {
                    return;
                }

                try
                {
                    await _schedulerLock.WaitAsync();

                    if (_scheduler != null)
                    {
                        return;
                    }

                    var factory = new StdSchedulerFactory();
                    _scheduler = await factory.GetScheduler();
                    _scheduler.JobFactory = _jobFactory;
                    await _scheduler.Start();
                }
                finally
                {
                    _schedulerLock.Release();
                }

                await SchedulePersistedJobsAsync();
            }
            catch (Exception e)
            {
                Log.Error(e, e.Message);
            }
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
                try
                {
                    Schedule(schedule.Id, schedule.CronTab);
                    Log.Information("Schedule {id} with cron tab {cronTab} scheduled.", schedule.Id, schedule.CronTab);
                }
                catch (Exception e)
                {
                    Log.Error("Unable to schedule {id} with cron tab {cronTab}: {reason}", schedule.Id, schedule.CronTab, e.Message);
                }
            }
        }
    }
}
