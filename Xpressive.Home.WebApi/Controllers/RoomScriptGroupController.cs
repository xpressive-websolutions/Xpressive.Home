using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using Xpressive.Home.Contracts.Rooms;

namespace Xpressive.Home.WebApi.Controllers
{
    [RoutePrefix("api/v1/roomscriptgroup")]
    public class RoomScriptGroupController : ApiController
    {
        private readonly IRoomRepository _roomRepository;
        private readonly IRoomScriptGroupRepository _repository;

        public RoomScriptGroupController(IRoomScriptGroupRepository repository, IRoomRepository roomRepository)
        {
            _repository = repository;
            _roomRepository = roomRepository;
        }

        [HttpGet, Route("{id}")]
        public async Task<IHttpActionResult> Get(string id)
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
        public async Task<IEnumerable<RoomScriptGroup>> GetByRoom([FromUri] string roomId)
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
    }
}
