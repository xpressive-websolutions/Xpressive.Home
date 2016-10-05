using Xpressive.Home.Contracts.Gateway;

namespace Xpressive.Home.Plugins.PhilipsHue
{
    internal sealed class PhilipsHuePresenceSensor : PhilipsHueDevice
    {
        private int _battery;

        public PhilipsHuePresenceSensor(string index, string id, string name, PhilipsHueBridge bridge) : base(index, id, name, bridge) { }

        public bool HasPresence { get; set; }

        internal int Battery
        {
            get { return _battery; }
            set
            {
                _battery = value;

                if (_battery > 85)
                {
                    BatteryStatus = DeviceBatteryStatus.Full;
                }
                else if (_battery > 25)
                {
                    BatteryStatus = DeviceBatteryStatus.Good;
                }
                else
                {
                    BatteryStatus = DeviceBatteryStatus.Low;
                }
            }
        }
    }
}
