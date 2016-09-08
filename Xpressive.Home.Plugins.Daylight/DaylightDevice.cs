using Xpressive.Home.Contracts.Gateway;

namespace Xpressive.Home.Plugins.Daylight
{
    internal class DaylightDevice : DeviceBase
    {
        [DeviceProperty(3)]
        public double Latitude { get; set; }

        [DeviceProperty(4)]
        public double Longitude { get; set; }

        [DeviceProperty(5)]
        public int OffsetInMinutes { get; set; }

        internal bool IsDaylight { get; set; }

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
