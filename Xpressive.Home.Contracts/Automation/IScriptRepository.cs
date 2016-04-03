using System.Collections.Generic;
using System.Threading.Tasks;

namespace Xpressive.Home.Contracts.Automation
{
    public interface IScriptRepository
    {
        Task<Script> GetAsync(string id);
        Task<IEnumerable<Script>> GetAsync(IEnumerable<string> ids);
        Task<IEnumerable<Script>> GetAsync();
        Task SaveAsync(Script script);
        Task DeleteAsync(Script script);
        Task DeleteAsync(string id);
    }
}