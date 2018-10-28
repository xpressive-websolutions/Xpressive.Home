using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xpressive.Home.Contracts.Automation;
using Xpressive.Home.Contracts.Rooms;
using Xpressive.Home.DatabaseModel;

namespace Xpressive.Home.Controllers
{
    [Route("api/v1/script")]
    public class ScriptController : Controller
    {
        private readonly XpressiveHomeContext _context;
        private readonly IRoomScriptRepository _roomScriptRepository;
        private readonly IScriptEngine _scriptEngine;

        public ScriptController(IRoomScriptRepository roomScriptRepository, IScriptEngine scriptEngine, XpressiveHomeContext context)
        {
            _roomScriptRepository = roomScriptRepository;
            _scriptEngine = scriptEngine;
            _context = context;
        }

        [HttpGet, Route("")]
        public async Task<IEnumerable<ScriptDto>> GetScripts()
        {
            var scripts = await _context.Script.ToListAsync();
            return scripts
                .OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
                .Select(s => new ScriptDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    IsEnabled = s.IsEnabled
                });
        }

        [HttpGet, Route("{id}")]
        public async Task<IActionResult> Get(string id)
        {
            if (Guid.TryParse(id, out Guid guid))
            {
                var script = await _context.Script.FindAsync(guid.ToString("n"));

                if (script == null)
                {
                    return NotFound();
                }

                return Ok(script);
            }

            return BadRequest();
        }

        [HttpPost, Route("{scriptId}/enable")]
        public async Task<IActionResult> Enable(string scriptId)
        {
            if (Guid.TryParse(scriptId, out Guid id))
            {
                var script = await _context.Script.FindAsync(id.ToString("n"));

                if (script != null)
                {
                    script.IsEnabled = true;
                    await _context.SaveChangesAsync();
                    return Ok();
                }
            }

            return NotFound();
        }

        [HttpPost, Route("{scriptId}/disable")]
        public async Task<IActionResult> Disable(string scriptId)
        {
            if (Guid.TryParse(scriptId, out Guid id))
            {
                var script = await _context.Script.FindAsync(id.ToString("n"));

                if (script != null)
                {
                    script.IsEnabled = false;
                    await _context.SaveChangesAsync();
                    return Ok();
                }
            }

            return NotFound();
        }

        [HttpGet, Route("group/{scriptGroupId}")]
        public async Task<IEnumerable<ScriptDto>> GetByScriptGroup(string scriptGroupId)
        {
            if (!Guid.TryParse(scriptGroupId, out Guid groupId))
            {
                return Enumerable.Empty<ScriptDto>();
            }

            var scripts = await _roomScriptRepository.GetAsync(groupId);

            return scripts
                .OrderBy(s => s.Name)
                .Select(s => new ScriptDto
                {
                    Id = s.ScriptId.ToString("n"),
                    Name = s.Name
                });
        }

        [HttpPost, Route("")]
        public async Task<IActionResult> Create([FromBody] string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return BadRequest();
            }

            var script = new Script
            {
                Id = Guid.NewGuid().ToString("n"),
                Name = name,
                JavaScript = string.Empty
            };

            _context.Script.Add(script);
            await _context.SaveChangesAsync();

            return Ok(script);
        }

        [HttpPost, Route("{id}")]
        public async Task Update(string id, [FromBody] Script script)
        {
            if (!Guid.TryParse(id, out Guid guid) || script == null)
            {
                return;
            }

            var persisted = await _context.Script.FindAsync(guid.ToString("n"));
            if (persisted == null)
            {
                return;
            }

            persisted.Name = script.Name;
            persisted.JavaScript = script.JavaScript;

            await _context.SaveChangesAsync();
        }

        [HttpPost, Route("execute/{scriptId}")]
        public async Task Execute(string scriptId)
        {
            if (Guid.TryParse(scriptId, out Guid id))
            {
                await _scriptEngine.ExecuteEvenIfDisabledAsync(id);
            }
        }

        [HttpDelete, Route("{scriptId}")]
        public async Task Delete(string scriptId)
        {
            if (Guid.TryParse(scriptId, out Guid id))
            {
                var script = await _context.Script.FindAsync(id.ToString("n"));
                if (script != null)
                {
                    _context.Script.Remove(script);
                    await _context.SaveChangesAsync();
                }
            }
        }

        public class ScriptDto
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public bool IsEnabled { get; set; }
        }
    }
}
