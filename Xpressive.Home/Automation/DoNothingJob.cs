using Quartz;
using System.Threading.Tasks;

namespace Xpressive.Home.Automation
{
    internal class DoNothingJob : IJob
    {
        public Task Execute(IJobExecutionContext context) { return Task.CompletedTask; }
    }
}
