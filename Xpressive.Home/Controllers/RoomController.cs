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
    [Route("api/v1/room")]
    public class RoomController : Controller
    {
        private readonly XpressiveHomeContext _context;

        public RoomController(XpressiveHomeContext context)
        {
            _context = context;
        }

        [HttpGet, Route("")]
        public async Task<IEnumerable<Room>> Get()
        {
            return await _context.Room.ToListAsync();
        }

        [HttpGet, Route("{id}")]
        public async Task<IActionResult> Get(string id)
        {
            var rooms = await _context.Room.ToListAsync();
            var room = rooms.SingleOrDefault(r => r.Id.Equals(id));

            if (room != null)
            {
                return Ok(room);
            }

            return NotFound();
        }

        [HttpPost, Route("")]
        public async Task<Room> Create([FromBody] string name)
        {
            var room = new Room
            {
                Id = Guid.NewGuid().ToString("n"),
                Name = name,
                Icon = string.Empty
            };

            _context.Room.Add(room);
            await _context.SaveChangesAsync();
            return room;
        }

        [HttpPut, Route("")]
        public async Task Update([FromBody] Room room)
        {
            if (string.IsNullOrEmpty(room.Id))
            {
                throw new ArgumentException("Id must not be empty", nameof(room));
            }

            var original = await _context.Room.FindAsync(room.Id);
            original.Name = room.Name;
            original.Icon = room.Icon;
            original.SortOrder = room.SortOrder;
            await _context.SaveChangesAsync();
        }

        [HttpDelete, Route("")]
        public async Task Delete([FromBody] Room room)
        {
            if (string.IsNullOrEmpty(room.Id))
            {
                throw new ArgumentException("Id must not be empty", nameof(room));
            }

            var original = await _context.Room.FindAsync(room.Id);
            _context.Room.Remove(original);
            await _context.SaveChangesAsync();
        }
    }
}
