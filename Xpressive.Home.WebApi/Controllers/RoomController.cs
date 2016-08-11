using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Http;
using Xpressive.Home.Contracts.Rooms;

namespace Xpressive.Home.WebApi.Controllers
{
    [RoutePrefix("api/v1/room")]
    public class RoomController : ApiController
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

        [HttpPost, Route("")]
        public async Task<Room> Create([FromBody] Room room)
        {
            room = new Room
            {
                Name = room.Name,
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
