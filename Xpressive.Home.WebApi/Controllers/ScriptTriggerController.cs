using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using Xpressive.Home.Contracts.Automation;

namespace Xpressive.Home.WebApi.Controllers
{
    [RoutePrefix("api/v1/trigger")]
    public class ScriptTriggerController : ApiController
    {
        private readonly IScriptTriggerService _triggerService;

        public ScriptTriggerController(IScriptTriggerService triggerService)
        {
            _triggerService = triggerService;
        }

        [HttpGet, Route("{scriptId}")]
        public async Task<IEnumerable<TriggeredScript>> GetAsync(string scriptId)
        {
            Guid id;
            if (Guid.TryParse(scriptId, out id))
            {
                return await _triggerService.GetTriggersByScriptAsync(id);
            }
            return Enumerable.Empty<TriggeredScript>();
        }

        [HttpPost, Route("{scriptId}")]
        public async Task<TriggeredScript> CreateAsync(string scriptId, [FromBody] string variable)
        {
            Guid id;
            if (!Guid.TryParse(scriptId, out id) || string.IsNullOrEmpty(variable))
            {
                return null;
            }

            return await _triggerService.AddTriggerAsync(id, variable);
        }

        [HttpDelete, Route("{triggerId}")]
        public async Task DeleteAsync(string triggerId)
        {
            Guid id;
            if (Guid.TryParse(triggerId, out id))
            {
                await _triggerService.DeleteTriggerAsync(id);
            }
        }
    }
}
