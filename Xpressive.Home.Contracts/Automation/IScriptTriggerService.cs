using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Xpressive.Home.Contracts.Automation
{
    public interface IScriptTriggerService
    {
        Task<IEnumerable<TriggeredScript>> GetTriggersAsync();
        Task<IEnumerable<TriggeredScript>> GetTriggersByVariableAsync(string variable);
        Task<IEnumerable<TriggeredScript>> GetTriggersByScriptAsync(Guid scriptId);

        Task<TriggeredScript> AddTriggerAsync(Guid scriptId, string variable);

        Task DeleteTriggerAsync(Guid id);
    }
}
