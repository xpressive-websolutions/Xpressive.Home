using Xpressive.Home.Contracts.Gateway;

namespace Xpressive.Home.Plugins.Denon
{
    internal class DenonDevice : DeviceBase
    {
        private readonly string _ipAddress;

        public DenonDevice(string id, string ipAddress)
        {
            _ipAddress = ipAddress;
            Id = id;
        }

        public string IpAddress => _ipAddress;
        public double Volume { get; set; }
        public bool IsMute { get; set; }
        public string Source { get; set; }
    }
}
