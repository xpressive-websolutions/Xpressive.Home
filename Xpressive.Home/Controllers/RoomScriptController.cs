using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Xpressive.Home.Contracts.Rooms;

namespace Xpressive.Home.Controllers
{
    [Route("api/v1/roomscript")]
    public class RoomScriptController : Controller
    {
        private readonly IRoomScriptRepository _repository;

        public RoomScriptController(IRoomScriptRepository repository)
        {
            _repository = repository;
        }

        [HttpGet, Route("")]
        public async Task<IEnumerable<RoomScript>> Get([FromQuery] string groupId)
        {
            return await _repository.GetAsync(new Guid(groupId));
        }

        [HttpPost, Route("")]
        public async Task Create([FromBody] RoomScript roomScript)
        {
            var script = new RoomScript
            {
                Id = Guid.Empty,
                GroupId = roomScript.GroupId,
                ScriptId = roomScript.ScriptId,
                Name = roomScript.Name
            };

            await _repository.SaveAsync(script);
        }

        [HttpPost, Route("{id}")]
        public async Task Update(string id, [FromBody] RoomScript roomScript)
        {
            var scripts = await _repository.GetAsync(roomScript.GroupId);
            var script = scripts.SingleOrDefault(s => s.Id.ToString("n").Equals(id, StringComparison.OrdinalIgnoreCase));

            if (script != null)
            {
                script.Name = roomScript.Name;
                script.ScriptId = roomScript.ScriptId;

                await _repository.SaveAsync(script);
            }
        }
    }
}
