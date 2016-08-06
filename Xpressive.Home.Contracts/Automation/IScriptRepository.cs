using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Xpressive.Home.Contracts.Automation
{
    public interface IScriptRepository
    {
        Task<Script> GetAsync(Guid id);
        Task<IEnumerable<Script>> GetAsync(IEnumerable<Guid> ids);
        Task<IEnumerable<Script>> GetAsync();
        Task SaveAsync(Script script);
        Task DeleteAsync(Script script);
        Task DeleteAsync(Guid id);
    }
}