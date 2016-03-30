using System.Collections.Generic;
using System.Threading.Tasks;

namespace Xpressive.Home.Contracts.Automation
{
    internal interface IScriptRepository
    {
        Task<Script> GetAsync(string id);
        Task<IEnumerable<Script>> GetAsync(IEnumerable<string> ids);
        Task<IEnumerable<Script>> GetAsync();
        Task SaveAsync(Script script);
        Task DeleteAsync(string id);
    }
}