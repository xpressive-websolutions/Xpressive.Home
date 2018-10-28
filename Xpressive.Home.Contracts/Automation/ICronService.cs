using System.Threading.Tasks;

namespace Xpressive.Home.Contracts.Automation
{
    public interface ICronService
    {
        Task<ScheduledScript> ScheduleAsync(string scriptId, string cronTab);

        Task DeleteScheduleAsync(string id);
    }
}
