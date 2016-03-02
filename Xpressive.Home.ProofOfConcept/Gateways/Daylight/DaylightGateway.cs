using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Xpressive.Home.ProofOfConcept.Gateways.Daylight
{
    internal class DaylightGateway : GatewayBase
    {
        public DaylightGateway() : base("Daylight")
        {
            _properties.Add(new BoolProperty("Daylight"));

            Observe();
        }

        protected override Task<string> GetInternal(DeviceBase device, PropertyBase property)
        {
            if (property.Name.Equals("Daylight", StringComparison.Ordinal))
            {
                return Task.FromResult(IsDaylight((DaylightDevice)device).ToString());
            }

            return Task.FromResult<string>(null);
        }

        protected override Task SetInternal(DeviceBase device, PropertyBase property, string value)
        {
            return Task.CompletedTask;
        }

        protected override Task ExecuteInternal(DeviceBase device, IAction action, IDictionary<string, string> values)
        {
            return Task.CompletedTask;
        }

        private async Task Observe()
        {
            var device = (DaylightDevice)_devices.Single();

            while (true)
            {
                await Task.Delay(TimeSpan.FromMinutes(1));

                var daylight = IsDaylight(device);
                OnDevicePropertyChanged(device, _properties.Single(), daylight.ToString());
            }
        }

        private bool IsDaylight(DaylightDevice device)
        {
            var time = System.DateTime.UtcNow.TimeOfDay;
            var sunrise = SunsetCalculator.GetSunrise(device.Latitude, device.Longitude);
            var sunset = SunsetCalculator.GetSunset(device.Latitude, device.Longitude);

            return time >= sunrise && time <= sunset;
        }
    }
}