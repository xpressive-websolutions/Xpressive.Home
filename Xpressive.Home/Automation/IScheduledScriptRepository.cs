using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xpressive.Home.Contracts.Automation;

namespace Xpressive.Home.Automation
{
    internal interface IScheduledScriptRepository
    {
        Task InsertAsync(Guid jobId, Guid scriptId, string cronTab);

        Task DeleteAsync(Guid id);

        Task<IEnumerable<ScheduledScript>> GetAsync();

        Task<ScheduledScript> GetAsync(Guid id);
    }
}
