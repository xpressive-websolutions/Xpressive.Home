using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NPoco;
using Xpressive.Home.Contracts.Rooms;

namespace Xpressive.Home.Services
{
    internal class RoomScriptRepository : IRoomScriptRepository
    {
        public async Task<IEnumerable<RoomScript>> GetAsync(RoomScriptGroup group)
        {
            using (var database = new Database("ConnectionString"))
            {
                var sql = "select * from RoomScript where GroupId = @0";
                return await database.FetchAsync<RoomScript>(sql, group.Id);
            }
        }

        public async Task SaveAsync(RoomScript roomScript)
        {
            using (var database = new Database("ConnectionString"))
            {
                if (roomScript.Id == Guid.Empty)
                {
                    roomScript.Id = Guid.NewGuid();
                    await database.InsertAsync(roomScript);
                }
                else
                {
                    await database.UpdateAsync(roomScript);
                }
            }
        }

        public async Task DeleteAsync(RoomScript roomScript)
        {
            using (var database = new Database("ConnectionString"))
            {
                await database.DeleteAsync(roomScript);
            }
        }
    }
}