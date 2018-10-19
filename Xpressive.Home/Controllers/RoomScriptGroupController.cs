using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Xpressive.Home.Contracts.Rooms;

namespace Xpressive.Home.Controllers
{
    [Route("api/v1/roomscriptgroup")]
    public class RoomScriptGroupController : Controller
    {
        private readonly IRoomRepository _roomRepository;
        private readonly IRoomScriptGroupRepository _repository;

        public RoomScriptGroupController(IRoomScriptGroupRepository repository, IRoomRepository roomRepository)
        {
            _repository = repository;
            _roomRepository = roomRepository;
        }

        [HttpGet, Route("{id}")]
        public async Task<IActionResult> Get(string id)
        {
            Guid guid;
            if (Guid.TryParse(id, out guid))
            {
                var group = await _repository.GetAsync(guid);

                if (group != null)
                {
                    return Ok(group);
                }
            }

            return NotFound();
        }

        [HttpGet, Route("")]
        public async Task<IEnumerable<RoomScriptGroup>> GetByRoom([FromQuery] string roomId)
        {
            var rooms = await _roomRepository.GetAsync();
            var room = rooms.SingleOrDefault(r => r.Id.Equals(new Guid(roomId)));

            if (room == null)
            {
                return Enumerable.Empty<RoomScriptGroup>();
            }

            var groups = await _repository.GetAsync(room);
            return groups;
        }

        [HttpPost, Route("{roomId}")]
        public async Task<RoomScriptGroup> Create(string roomId, [FromBody] RoomScriptGroup group)
        {
            var rooms = await _roomRepository.GetAsync();
            var room = rooms.SingleOrDefault(r => r.Id.Equals(new Guid(roomId)));

            if (room == null)
            {
                return null;
            }

            group = new RoomScriptGroup
            {
                Name = group.Name,
                Icon = string.Empty,
                RoomId = room.Id
            };

            await _repository.SaveAsync(group);

            return group;
        }

        [HttpPost, Route("")]
        public async Task<IActionResult> Save([FromBody] RoomScriptGroup group)
        {
            if (group == null || group.Id == Guid.Empty)
            {
                return NotFound();
            }

            var existing = await _repository.GetAsync(group.Id);

            if (existing == null)
            {
                return NotFound();
            }

            existing.Icon = group.Icon;
            existing.Name = group.Name;
            existing.SortOrder = group.SortOrder;

            await _repository.SaveAsync(existing);
            return Ok();
        }
    }
}
