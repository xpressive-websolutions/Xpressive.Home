using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Xpressive.Home.Contracts.Automation;

namespace Xpressive.Home.Controllers
{
    [Route("api/v1/trigger")]
    public class ScriptTriggerController : Controller
    {
        private readonly IScriptTriggerService _triggerService;

        public ScriptTriggerController(IScriptTriggerService triggerService)
        {
            _triggerService = triggerService;
        }

        [HttpGet, Route("{scriptId}")]
        public async Task<IEnumerable<TriggeredScript>> GetAsync(string scriptId)
        {
            return await _triggerService.GetTriggersByScriptAsync(scriptId);
        }

        [HttpPost, Route("{scriptId}")]
        public async Task<TriggeredScript> CreateAsync(string scriptId, [FromBody] string variable)
        {
            return await _triggerService.AddTriggerAsync(scriptId, variable);
        }

        [HttpDelete, Route("{triggerId}")]
        public async Task DeleteAsync(string triggerId)
        {
            await _triggerService.DeleteTriggerAsync(triggerId);
        }
    }
}
