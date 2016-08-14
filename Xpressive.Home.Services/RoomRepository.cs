using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NPoco;
using Xpressive.Home.Contracts.Rooms;

namespace Xpressive.Home.Services
{
    internal class RoomRepository : IRoomRepository
    {
        private readonly IRoomScriptGroupRepository _roomScriptGroupRepository;
        private readonly IRoomScriptRepository _roomScriptRepository;

        public RoomRepository(IRoomScriptGroupRepository roomScriptGroupRepository, IRoomScriptRepository roomScriptRepository)
        {
            _roomScriptGroupRepository = roomScriptGroupRepository;
            _roomScriptRepository = roomScriptRepository;
        }

        public async Task<IEnumerable<Room>> GetAsync()
        {
            using (var database = new Database("ConnectionString"))
            {
                var rooms =  await database.FetchAsync<Room>("select * from Room");
                return rooms
                    .OrderBy(r => r.SortOrder)
                    .ThenBy(r => r.Name, StringComparer.OrdinalIgnoreCase);
            }
        }

        public async Task SaveAsync(Room room)
        {
            using (var database = new Database("ConnectionString"))
            {
                if (room.Id == Guid.Empty)
                {
                    room.Id = Guid.NewGuid();
                    await database.InsertAsync(room);
                }
                else
                {
                    await database.UpdateAsync(room);
                }
            }
        }

        public async Task DeleteAsync(Room room)
        {
            var groups = (await _roomScriptGroupRepository.GetAsync(room)).ToList();
            var scripts = new List<RoomScript>();

            foreach (var group in groups)
            {
                scripts.AddRange(await _roomScriptRepository.GetAsync(group.Id));
            }

            using (var database = new Database("ConnectionString"))
            {
                using (var transaction = database.GetTransaction())
                {
                    foreach (var script in scripts)
                    {
                        await database.DeleteAsync(script);
                    }

                    foreach (var group in groups)
                    {
                        await database.DeleteAsync(group);
                    }

                    await database.DeleteAsync(room);

                    transaction.Complete();
                }
            }
        }
    }
}