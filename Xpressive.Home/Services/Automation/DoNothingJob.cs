using System.Threading.Tasks;
using Quartz;

namespace Xpressive.Home.Services.Automation
{
    internal class DoNothingJob : IJob
    {
        public Task Execute(IJobExecutionContext context) { return Task.CompletedTask; }
    }
}
