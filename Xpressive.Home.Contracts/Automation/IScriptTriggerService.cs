using System.Collections.Generic;
using System.Threading.Tasks;

namespace Xpressive.Home.Contracts.Automation
{
    public interface IScriptTriggerService
    {
        Task<IEnumerable<TriggeredScript>> GetTriggersAsync();
        Task<IEnumerable<TriggeredScript>> GetTriggersByVariableAsync(string variable);
        Task<IEnumerable<TriggeredScript>> GetTriggersByScriptAsync(string scriptId);

        Task<TriggeredScript> AddTriggerAsync(string scriptId, string variable);

        Task DeleteTriggerAsync(string id);
    }
}
