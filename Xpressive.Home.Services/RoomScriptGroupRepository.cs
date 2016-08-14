using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NPoco;
using Xpressive.Home.Contracts.Rooms;

namespace Xpressive.Home.Services
{
    internal class RoomScriptGroupRepository : IRoomScriptGroupRepository
    {
        private readonly IRoomScriptRepository _roomScriptRepository;

        public RoomScriptGroupRepository(IRoomScriptRepository roomScriptRepository)
        {
            _roomScriptRepository = roomScriptRepository;
        }

        public async Task<RoomScriptGroup> GetAsync(Guid id)
        {
            using (var database = new Database("ConnectionString"))
            {
                return await database.SingleOrDefaultByIdAsync<RoomScriptGroup>(id);
            }
        }

        public async Task<IEnumerable<RoomScriptGroup>> GetAsync(Room room)
        {
            using (var database = new Database("ConnectionString"))
            {
                var sql = "select * from RoomScriptGroup where RoomId = @0";
                return await database.FetchAsync<RoomScriptGroup>(sql, room.Id);
            }
        }

        public async Task SaveAsync(RoomScriptGroup group)
        {
            using (var database = new Database("ConnectionString"))
            {
                if (group.Id == Guid.Empty)
                {
                    group.Id = Guid.NewGuid();
                    await database.InsertAsync(group);
                }
                else
                {
                    await database.UpdateAsync(group);
                }
            }
        }

        public async Task DeleteAsync(RoomScriptGroup group)
        {
            var scripts = await _roomScriptRepository.GetAsync(group.Id);

            using (var database = new Database("ConnectionString"))
            {
                using (var transaction = database.GetTransaction())
                {
                    foreach (var script in scripts)
                    {
                        await database.DeleteAsync(script);
                    }

                    await database.DeleteAsync(group);

                    transaction.Complete();
                }
            }
        }
    }
}