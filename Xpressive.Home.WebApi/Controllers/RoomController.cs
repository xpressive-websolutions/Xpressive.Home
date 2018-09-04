using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Xpressive.Home.Contracts.Rooms;

namespace Xpressive.Home.WebApi.Controllers
{
    [Route("api/v1/room")]
    public class RoomController : Controller
    {
        private readonly IRoomRepository _repository;

        public RoomController(IRoomRepository repository)
        {
            _repository = repository;
        }

        [HttpGet, Route("")]
        public async Task<IEnumerable<Room>> Get()
        {
            return await _repository.GetAsync();
        }

        [HttpGet, Route("{id}")]
        public async Task<IActionResult> Get(string id)
        {
            Guid guid;
            if (Guid.TryParse(id, out guid))
            {
                var rooms = await _repository.GetAsync();
                var room = rooms.SingleOrDefault(r => r.Id.Equals(guid));

                if (room != null)
                {
                    return Ok(room);
                }
            }

            return NotFound();
        }

        [HttpPost, Route("")]
        public async Task<Room> Create([FromBody] string name)
        {
            var room = new Room
            {
                Name = name,
                Icon = string.Empty
            };

            await _repository.SaveAsync(room);
            return room;
        }

        [HttpPut, Route("")]
        public async Task Update([FromBody] Room room)
        {
            if (room.Id == Guid.Empty)
            {
                throw new ArgumentException("Id must not be empty", nameof(room));
            }

            await _repository.SaveAsync(room);
        }

        [HttpDelete, Route("")]
        public async Task Delete([FromBody] Room room)
        {
            if (room.Id == Guid.Empty)
            {
                throw new ArgumentException("Id must not be empty", nameof(room));
            }

            await _repository.DeleteAsync(room);
        }
    }
}
