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
    [Route("api/v1/roomscriptgroup")]
    public class RoomScriptGroupController : Controller
    {
        private readonly XpressiveHomeContext _context;

        public RoomScriptGroupController(XpressiveHomeContext context)
        {
            _context = context;
        }

        [HttpGet, Route("{id}")]
        public async Task<IActionResult> Get(string id)
        {
            var group = await _context.RoomScriptGroup.FindAsync(id);

            if (group != null)
            {
                return Ok(group);
            }

            return NotFound();
        }

        [HttpGet, Route("")]
        public async Task<IEnumerable<RoomScriptGroup>> GetByRoom([FromQuery] string roomId)
        {
            var groups = await _context.RoomScriptGroup.Where(g => g.RoomId == roomId).ToListAsync();
            return groups;
        }

        [HttpPost, Route("{roomId}")]
        public async Task<RoomScriptGroup> Create(string roomId, [FromBody] RoomScriptGroup group)
        {
            var room = await _context.Room.FindAsync(roomId);

            if (room == null)
            {
                return null;
            }

            group = new RoomScriptGroup
            {
                Id = Guid.NewGuid().ToString("n"),
                Name = group.Name,
                Icon = string.Empty,
                RoomId = room.Id
            };

            _context.RoomScriptGroup.Add(group);
            await _context.SaveChangesAsync();

            return group;
        }

        [HttpPost, Route("")]
        public async Task<IActionResult> Save([FromBody] RoomScriptGroup group)
        {
            if (string.IsNullOrEmpty(group?.Id))
            {
                return NotFound();
            }

            var existing = await _context.RoomScriptGroup.FindAsync(group.Id);

            if (existing == null)
            {
                return NotFound();
            }

            existing.Icon = group.Icon;
            existing.Name = group.Name;
            existing.SortOrder = group.SortOrder;

            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}
