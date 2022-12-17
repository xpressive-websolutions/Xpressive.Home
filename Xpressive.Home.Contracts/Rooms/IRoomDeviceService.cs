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
}
