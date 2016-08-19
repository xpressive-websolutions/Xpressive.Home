using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using Xpressive.Home.Contracts.Automation;

namespace Xpressive.Home.WebApi.Controllers
{
    [RoutePrefix("api/v1/schedule")]
    public class ScriptSchedulerController : ApiController
    {
        private readonly ICronService _cronService;
        private readonly IScheduledScriptRepository _repository;

        public ScriptSchedulerController(ICronService cronService, IScheduledScriptRepository repository)
        {
            _cronService = cronService;
            _repository = repository;
        }

        [HttpGet, Route("{scriptId}")]
        public async Task<IEnumerable<ScheduledScript>> GetAsync(string scriptId)
        {
            Guid id;
            if (Guid.TryParse(scriptId, out id))
            {
                var scripts = await _repository.GetAsync();
                return scripts.Where(s => s.ScriptId.Equals(id));
            }
            return Enumerable.Empty<ScheduledScript>();
        }

        [HttpPost, Route("{scriptId}")]
        public async Task ScheduleAsync(string scriptId, [FromBody]string cronTab)
        {
            Guid id;
            if (Guid.TryParse(scriptId, out id))
            {
                await _cronService.ScheduleAsync(id, cronTab);
            }
        }

        [HttpDelete, Route("{scheduleId}")]
        public async Task DeleteAsync(string scheduleId)
        {
            Guid id;
            if (Guid.TryParse(scheduleId, out id))
            {
                await _cronService.DeleteScheduleAsync(id);
            }
        }
    }
}
