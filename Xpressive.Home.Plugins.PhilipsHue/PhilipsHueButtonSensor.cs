using System;
using Xpressive.Home.Contracts.Gateway;

namespace Xpressive.Home.Plugins.PhilipsHue
{
    internal sealed class PhilipsHueButtonSensor : PhilipsHueDevice
    {
        private int _battery;

        public PhilipsHueButtonSensor(string index, string id, string name, PhilipsHueBridge bridge) : base(index, id, name, bridge) { }

        public int LastButton { get; set; }

        public DateTime LastButtonOccurrence { get; set; }

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
