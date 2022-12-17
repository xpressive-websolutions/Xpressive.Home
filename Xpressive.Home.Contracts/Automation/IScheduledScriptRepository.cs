using System.Collections.Generic;
using System.Threading.Tasks;

namespace Xpressive.Home.Contracts.Automation
{
    public interface IScheduledScriptRepository
    {
        Task InsertAsync(string jobId, string scriptId, string cronTab);

        Task DeleteAsync(string id);

        Task<IEnumerable<ScheduledScript>> GetAsync();

        Task<ScheduledScript> GetAsync(string id);
    }
}
