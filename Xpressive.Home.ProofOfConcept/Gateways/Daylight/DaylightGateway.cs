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
            _properties.Add("Daylight");

            Observe();
        }

        protected override Task<string> GetInternal(IDevice device, string property)
        {
            if (property.Equals("Daylight", StringComparison.Ordinal))
            {
                return Task.FromResult(IsDaylight((DaylightDevice)device).ToString());
            }

            return Task.FromResult<string>(null);
        }

        protected override Task ExecuteInternal(IDevice device, IAction action, IDictionary<string, string> values)
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