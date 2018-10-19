using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Xpressive.Home.Contracts.Rooms;

namespace Xpressive.Home.Controllers
{
    [Route("api/v1/roomdevice")]
    public class RoomDeviceController : Controller
    {
        private readonly IRoomDeviceService _service;

        public RoomDeviceController(IRoomDeviceService service)
        {
            _service = service;
        }

        [HttpGet, Route("{gatewayName}")]
        public async Task<IEnumerable<PairingDto>> Get(string gatewayName)
        {
            var roomDevices = await _service.GetRoomDevicesAsync(gatewayName);

            return roomDevices.Select(r => new PairingDto
            {
                GatewayName = r.Gateway,
                DeviceId = r.Id,
                RoomId = r.RoomId.ToString("d")
            });
        }

        [HttpPost, Route("")]
        public async Task Pair([FromBody] PairingDto dto)
        {
            await _service.AddDeviceToRoomAsync(dto.GatewayName, dto.DeviceId, dto.RoomId);
        }

        [HttpDelete, Route("")]
        public async Task RemovePair([FromBody] PairingDto dto)
        {
            await _service.RemoveDeviceFromRoomAsync(dto.GatewayName, dto.DeviceId);
        }

        public class PairingDto
        {
            public string GatewayName { get; set; }
            public string DeviceId { get; set; }
            public string RoomId { get; set; }
        }
    }
}
