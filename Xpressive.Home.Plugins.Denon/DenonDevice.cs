using Xpressive.Home.Contracts.Gateway;

namespace Xpressive.Home.Plugins.Denon
{
    internal class DenonDevice : DeviceBase
    {
        private readonly string _ipAddress;

        public DenonDevice(string ipAddress, DenonDeviceDto dto)
        {
            _ipAddress = ipAddress;

            Name = dto.FriendlyName.Value;
            Id = ipAddress.Replace(".", string.Empty);
        }

        public string IpAddress => _ipAddress;
    }
}