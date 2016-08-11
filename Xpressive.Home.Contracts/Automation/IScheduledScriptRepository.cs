using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Xpressive.Home.Contracts.Automation
{
    public interface IScheduledScriptRepository
    {
        Task InsertAsync(Guid jobId, Guid scriptId, string cronTab);

        Task DeleteAsync(Guid id);

        Task<IEnumerable<ScheduledScript>> GetAsync();

        Task<ScheduledScript> GetAsync(Guid id);
    }
}
