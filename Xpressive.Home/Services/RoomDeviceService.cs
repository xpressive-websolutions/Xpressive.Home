using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xpressive.Home.Contracts.Rooms;
using Xpressive.Home.DatabaseModel;

namespace Xpressive.Home.Services
{
    internal class RoomDeviceService : IRoomDeviceService
    {
        private readonly IContextFactory _contextFactory;

        public RoomDeviceService(IContextFactory contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<IEnumerable<RoomDevice>> GetRoomDevicesAsync(string gatewayName)
        {
            return await _contextFactory.InScope(async context => await context.RoomDevice.Where(d => d.Gateway == gatewayName).ToListAsync());
        }

        public async Task AddDeviceToRoomAsync(string gatewayName, string deviceId, string roomId)
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

            await _contextFactory.InScope(async context => await context.Database.ExecuteSqlCommandAsync(sql, gatewayName, deviceId, roomId));
        }

        public async Task RemoveDeviceFromRoomAsync(string gatewayName, string deviceId)
        {
            await _contextFactory.InScope(async context =>
            {
                var result = await context.RoomDevice.Where(rd => rd.Gateway == gatewayName && rd.Id == deviceId).ToListAsync();
                context.RoomDevice.RemoveRange(result);
                await context.SaveChangesAsync();
            });
        }
    }
}
