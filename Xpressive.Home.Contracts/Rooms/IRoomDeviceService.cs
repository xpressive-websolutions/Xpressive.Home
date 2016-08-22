using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Xpressive.Home.Contracts.Rooms
{
    public interface IRoomDeviceService
    {
        Task<IEnumerable<RoomDevice>> GetRoomDevicesAsync(string gatewayName);

        Task AddDeviceToRoomAsync(string gatewayName, string deviceId, string roomId);

        Task RemoveDeviceFromRoomAsync(string gatewayName, string deviceId);
    }

    public class RoomDevice
    {
        public string Gateway { get; set; }
        public string Id { get; set; }
        public Guid RoomId { get; set; }
    }
}
