using Xpressive.Home.Contracts.Gateway;

namespace Xpressive.Home.Plugins.PhilipsHue
{
    internal class PhilipsHueDevice : DeviceBase
    {
        private readonly string _index;
        private readonly PhilipsHueBridge _bridge;

        public PhilipsHueDevice(string index, string id, string name, PhilipsHueBridge bridge)
        {
            Id = id;
            Name = name;
            _index = index;
            _bridge = bridge;
        }

        public PhilipsHueBridge Bridge => _bridge;
        public string Index => _index;
    }
}
