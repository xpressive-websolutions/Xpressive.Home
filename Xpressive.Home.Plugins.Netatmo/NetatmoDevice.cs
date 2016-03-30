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
    }
}