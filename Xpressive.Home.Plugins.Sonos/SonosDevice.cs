using System.Collections.Generic;
using Xpressive.Home.Contracts.Gateway;

namespace Xpressive.Home.Plugins.Sonos
{
    public class SonosDevice : DeviceBase
    {
        private readonly string _ipAddress;

        public SonosDevice(string id, string ipAddress, string name)
        {
            _ipAddress = ipAddress;
            Id = id;
            Name = name;
            Services = new List<UpnpService>();
        }

        public string IpAddress => _ipAddress;
        public string Type { get; set; }
        public string Zone { get; set; }
        public bool IsMaster { get; set; }
        public List<UpnpService> Services { get; }
    }
}
