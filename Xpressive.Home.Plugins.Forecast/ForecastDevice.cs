using Xpressive.Home.Contracts.Gateway;

namespace Xpressive.Home.Plugins.Forecast
{
    public class ForecastDevice : DeviceBase
    {
        public ForecastDevice()
        {
            Icon = "fa fa-sun-o";
        }

        [DeviceProperty(3)]
        public double Latitude { get; set; }

        [DeviceProperty(4)]
        public double Longitude { get; set; }
    }
}