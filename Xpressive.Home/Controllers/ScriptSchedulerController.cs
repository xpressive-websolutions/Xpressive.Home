using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CronExpressionDescriptor;
using Microsoft.AspNetCore.Mvc;
using Xpressive.Home.Contracts.Automation;

namespace Xpressive.Home.Controllers
{
    [Route("api/v1/schedule")]
    public class ScriptSchedulerController : Controller
    {
        private readonly ICronService _cronService;
        private readonly IScheduledScriptRepository _repository;

        public ScriptSchedulerController(ICronService cronService, IScheduledScriptRepository repository)
        {
            _cronService = cronService;
            _repository = repository;
        }

        [HttpGet, Route("{scriptId}")]
        public async Task<IEnumerable<ScheduledScriptDto>> GetAsync(string scriptId)
        {
            var scripts = await _repository.GetAsync();
            return scripts
                .Where(s => s.ScriptId.Equals(scriptId))
                .Select(s => new ScheduledScriptDto(s));
        }

        [HttpPost, Route("{scriptId}")]
        public async Task ScheduleAsync(string scriptId, [FromBody]string cronTab)
        {
            await _cronService.ScheduleAsync(scriptId, cronTab);
        }

        [HttpDelete, Route("{scheduleId}")]
        public async Task DeleteAsync(string scheduleId)
        {
            await _cronService.DeleteScheduleAsync(scheduleId);
        }

        public class ScheduledScriptDto
        {
            public ScheduledScriptDto() { }

            public ScheduledScriptDto(ScheduledScript script)
            {
                var descriptionOptions = new Options
                {
                    ThrowExceptionOnParseError = false,
                    Use24HourTimeFormat = true
                };

                Id = script.Id;
                ScriptId = script.ScriptId;
                CronTab = script.CronTab;
                CronDescription = ExpressionDescriptor.GetDescription(script.CronTab, descriptionOptions);
            }

            public string Id { get; set; }
            public string ScriptId { get; set; }
            public string CronTab { get; set; }
            public string CronDescription { get; set; }
        }
    }
}
