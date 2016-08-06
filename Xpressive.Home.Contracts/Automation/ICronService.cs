using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Xpressive.Home.Contracts.Automation
{
    public interface ICronService
    {
        Task<IEnumerable<ScheduledScript>> GetSchedulesAsync();

        Task<ScheduledScript> ScheduleAsync(Guid scriptId, string cronTab);

        Task DeleteScheduleAsync(Guid id);
    }
}
