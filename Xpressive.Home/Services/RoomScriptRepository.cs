using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using NPoco;
using Xpressive.Home.Contracts.Rooms;

namespace Xpressive.Home.Services
{
    internal class RoomScriptRepository : IRoomScriptRepository
    {
        private readonly DbConnection _dbConnection;

        public RoomScriptRepository(DbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public async Task<IEnumerable<RoomScript>> GetAsync(Guid groupId)
        {
            using (var database = new Database(_dbConnection))
            {
                const string sql = "select * from RoomScript where GroupId = @0";
                var scripts = await database.FetchAsync<RoomScript>(sql, groupId);
                return scripts.OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase);
            }
        }

        public async Task SaveAsync(RoomScript roomScript)
        {
            using (var database = new Database(_dbConnection))
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
            using (var database = new Database(_dbConnection))
            {
                await database.DeleteAsync(roomScript);
            }
        }
    }
}