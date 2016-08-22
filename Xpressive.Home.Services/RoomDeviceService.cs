using System.Collections.Generic;
using System.Threading.Tasks;
using NPoco;
using Xpressive.Home.Contracts.Rooms;

namespace Xpressive.Home.Services
{
    internal class RoomDeviceService : IRoomDeviceService
    {
        public async Task<IEnumerable<RoomDevice>> GetRoomDevicesAsync(string gatewayName)
        {
            using (var database = new Database("ConnectionString"))
            {
                const string sql = "select Gateway, Id, RoomId from RoomDevice where Gateway = @0";
                return await database.FetchAsync<RoomDevice>(sql, gatewayName);
            }
        }

        public async Task AddDeviceToRoomAsync(string gatewayName, string deviceId, string roomId)
        {
            using (var database = new Database("ConnectionString"))
            {
                const string sql = @"
if (select count(*) from RoomDevice where Gateway = @0 and Id = @1) > 0
begin
  update RoomDevice set RoomId = @2 where Gateway = @0 and Id = @1
end
else
begin
  insert into RoomDevice (Gateway, Id, RoomId) values (@0, @1, @2)
end";

                await database.ExecuteAsync(sql, gatewayName, deviceId, roomId);
            }
        }

        public async Task RemoveDeviceFromRoomAsync(string gatewayName, string deviceId)
        {
            using (var database = new Database("ConnectionString"))
            {
                const string sql = "delete from RoomDevice where Gateway = @0 and Id = @1)";
                await database.ExecuteAsync(sql, gatewayName, deviceId);
            }
        }
    }
}
