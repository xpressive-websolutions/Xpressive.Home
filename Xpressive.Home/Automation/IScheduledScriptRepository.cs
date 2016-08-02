using System.Collections.Generic;
using System.Threading.Tasks;
using Xpressive.Home.Contracts.Automation;

namespace Xpressive.Home.Automation
{
    internal interface IScheduledScriptRepository
    {
        Task InsertAsync(string jobId, string scriptId, string cronTab);

        Task DeleteAsync(string id);

        Task<IEnumerable<ScheduledScript>> GetAsync();

        Task<ScheduledScript> GetAsync(string id);
    }
}
