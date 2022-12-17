using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xpressive.Home.Contracts.Rooms;
using Xpressive.Home.DatabaseModel;

namespace Xpressive.Home.Controllers
{
    [Route("api/v1/roomscript")]
    public class RoomScriptController : Controller
    {
        private readonly XpressiveHomeContext _context;

        public RoomScriptController(XpressiveHomeContext context)
        {
            _context = context;
        }

        [HttpGet, Route("")]
        public async Task<IEnumerable<RoomScript>> Get([FromQuery] string groupId)
        {
            return await _context.RoomScript.Where(rs => rs.GroupId == groupId).ToListAsync();
        }

        [HttpPost, Route("")]
        public async Task Create([FromBody] RoomScript roomScript)
        {
            var script = new RoomScript
            {
                Id = Guid.NewGuid().ToString("n"),
                GroupId = roomScript.GroupId,
                ScriptId = roomScript.ScriptId,
                Name = roomScript.Name
            };

            _context.RoomScript.Add(script);
            await _context.SaveChangesAsync();
        }

        [HttpPost, Route("{id}")]
        public async Task Update(string id, [FromBody] RoomScript roomScript)
        {
            var scripts = await _context.RoomScript.Where(rs => rs.GroupId == roomScript.GroupId).ToListAsync();
            var script = scripts.SingleOrDefault(s => s.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

            if (script != null)
            {
                script.Name = roomScript.Name;
                script.ScriptId = roomScript.ScriptId;

                await _context.SaveChangesAsync();
            }
        }
    }
}
