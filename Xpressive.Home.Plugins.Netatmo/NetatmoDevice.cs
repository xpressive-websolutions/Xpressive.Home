using Xpressive.Home.Contracts.Gateway;

namespace Xpressive.Home.Plugins.Netatmo
{
    internal class NetatmoDevice : DeviceBase
    {
        public NetatmoDevice(string id, string name)
        {
            Id = id;
            Name = name;
        }

        public double? Co2 { get; set; }
        public double? Humidity { get; set; }
        public double? Noise { get; set; }
        public double? Pressure { get; set; }
        public double? Temperature { get; set; }
    }
}
