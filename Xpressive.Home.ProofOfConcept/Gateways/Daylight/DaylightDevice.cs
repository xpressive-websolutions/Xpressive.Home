namespace Xpressive.Home.ProofOfConcept.Gateways.Daylight
{
    internal class DaylightDevice : DeviceBase
    {
        private readonly double _latitude;
        private readonly double _longitude;

        public DaylightDevice(double latitude, double longitude) : base("DaylightDevice", "DaylightDevice")
        {
            _latitude = latitude;
            _longitude = longitude;
        }

        public double Latitude => _latitude;
        public double Longitude => _longitude;
    }
}