using Xpressive.Home.Contracts.Gateway;

namespace Xpressive.Home.Plugins.PhilipsHue
{
    internal class PhilipsHueDevice : DeviceBase
    {
        private readonly PhilipsHueBridge _bridge;

        public PhilipsHueDevice(string id, string name, PhilipsHueBridge bridge)
        {
            Id = id;
            Name = name;
            _bridge = bridge;
        }

        public PhilipsHueBridge Bridge => _bridge;
    }
}