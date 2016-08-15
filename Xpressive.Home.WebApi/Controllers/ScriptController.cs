using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using Xpressive.Home.Contracts.Automation;
using Xpressive.Home.Contracts.Rooms;

namespace Xpressive.Home.WebApi.Controllers
{
    [RoutePrefix("api/v1/script")]
    public class ScriptController : ApiController
    {
        private readonly IScriptRepository _repository;
        private readonly IRoomScriptRepository _roomScriptRepository;
        private readonly IScriptEngine _scriptEngine;

        public ScriptController(IScriptRepository repository, IRoomScriptRepository roomScriptRepository, IScriptEngine scriptEngine)
        {
            _repository = repository;
            _roomScriptRepository = roomScriptRepository;
            _scriptEngine = scriptEngine;
        }

        [HttpGet, Route("")]
        public async Task<IEnumerable<NameIdDto>> GetScripts()
        {
            var scripts = await _repository.GetAsync();
            return scripts.Select(s => new NameIdDto { Id = s.Id.ToString("n"), Name = s.Name });
        }

        [HttpGet, Route("{id}")]
        public async Task<IHttpActionResult> Get(string id)
        {
            Guid guid;
            if (Guid.TryParse(id, out guid))
            {
                var script = await _repository.GetAsync(guid);

                if (script == null)
                {
                    return NotFound();
                }

                return Ok(script);
            }

            return BadRequest();
        }

        [HttpPost, Route("{scriptId}/enable")]
        public async Task<IHttpActionResult> Enable(string scriptId)
        {
            Guid id;
            if (Guid.TryParse(scriptId, out id))
            {
                var script = await _repository.GetAsync(id);

                if (script != null)
                {
                    await _repository.EnableAsync(script);
                    return Ok();
                }
            }

            return NotFound();
        }

        [HttpPost, Route("{scriptId}/disable")]
        public async Task<IHttpActionResult> Disable(string scriptId)
        {
            Guid id;
            if (Guid.TryParse(scriptId, out id))
            {
                var script = await _repository.GetAsync(id);

                if (script != null)
                {
                    await _repository.DisableAsync(script);
                    return Ok();
                }
            }

            return NotFound();
        }

        [HttpGet, Route("group/{scriptGroupId}")]
        public async Task<IEnumerable<NameIdDto>> GetByScriptGroup(string scriptGroupId)
        {
            Guid groupId;
            if (!Guid.TryParse(scriptGroupId, out groupId))
            {
                return Enumerable.Empty<NameIdDto>();
            }

            var scripts = await _roomScriptRepository.GetAsync(groupId);

            return scripts
                .OrderBy(s => s.Name)
                .Select(s => new NameIdDto
                {
                    Id = s.ScriptId.ToString("n"),
                    Name = s.Name
                });
        }

        [HttpPost, Route("")]
        public async Task<IHttpActionResult> Create([FromBody] string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return BadRequest();
            }

            var script = new Script
            {
                Name = name,
                JavaScript = string.Empty
            };

            await _repository.SaveAsync(script);

            return Ok(script);
        }

        [HttpPost, Route("{id}")]
        public async Task Update(string id, [FromBody] Script script)
        {
            Guid guid;
            if (!Guid.TryParse(id, out guid) || script == null)
            {
                return;
            }

            var persisted = await _repository.GetAsync(guid);
            if (persisted == null)
            {
                return;
            }

            persisted.Name = script.Name;
            persisted.JavaScript = script.JavaScript;

            await _repository.SaveAsync(persisted);
        }
        
        [HttpPost, Route("execute/{scriptId}")]
        public async Task Execute(string scriptId)
        {
            Guid id;
            if (Guid.TryParse(scriptId, out id))
            {
                await _scriptEngine.ExecuteEvenIfDisabledAsync(id);
            }
        }

        [HttpDelete, Route("{scriptId}")]
        public async Task Delete(string scriptId)
        {
            Guid id;
            if (Guid.TryParse(scriptId, out id))
            {
                await _repository.DeleteAsync(id);
            }
        }

        public class NameIdDto
        {
            public string Id { get; set; }
            public string Name { get; set; }
        }
    }
}
