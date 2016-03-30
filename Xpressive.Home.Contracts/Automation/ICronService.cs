using System.Collections.Generic;
using System.Threading.Tasks;

namespace Xpressive.Home.Contracts.Automation
{
    public interface ICronService
    {
        Task<IEnumerable<ScheduledScript>> GetSchedules();

        Task ScheduleAsync(string scriptId, string cronTab);

        Task DeleteSchedule(string id);
    }
}