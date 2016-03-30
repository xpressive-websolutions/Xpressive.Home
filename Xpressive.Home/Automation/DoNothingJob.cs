using Quartz;

namespace Xpressive.Home.Automation
{
    internal class DoNothingJob : IJob
    {
        public void Execute(IJobExecutionContext context) { }
    }
}