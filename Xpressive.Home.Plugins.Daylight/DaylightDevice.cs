using Xpressive.Home.Contracts;
using Xpressive.Home.Contracts.Gateway;

namespace Xpressive.Home.Plugins.Daylight
{
    internal class DaylightDevice : DeviceBase
    {
        [DeviceProperty]
        public double Latitude { get; set; }

        [DeviceProperty]
        public double Longitude { get; set; }

        [DeviceProperty]
        public int Offset { get; set; }

        public override bool IsConfigurationValid()
        {
            if (Longitude < -180 || Longitude > 180)
            {
                return false;
            }

            if (Latitude < -90 || Latitude > 90)
            {
                return false;
            }

            return base.IsConfigurationValid();
        }
    }
}