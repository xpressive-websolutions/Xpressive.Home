using Xpressive.Home.Contracts.Gateway;

namespace Xpressive.Home.Plugins.Denon
{
    internal class DenonDevice : DeviceBase
    {
        private readonly string _ipAddress;

        public DenonDevice(string id, string ipAddress, DenonDeviceDto dto)
        {
            _ipAddress = ipAddress;

            Name = dto.FriendlyName.Value;
            Id = id;
        }

        public string IpAddress => _ipAddress;
    }
}