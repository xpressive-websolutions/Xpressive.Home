using System;
using System.Threading.Tasks;

namespace Xpressive.Home.Contracts.Automation
{
    public interface ICronService
    {
        Task<ScheduledScript> ScheduleAsync(Guid scriptId, string cronTab);

        Task DeleteScheduleAsync(Guid id);
    }
}
