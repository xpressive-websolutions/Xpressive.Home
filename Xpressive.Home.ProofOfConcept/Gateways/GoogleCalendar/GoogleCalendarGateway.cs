using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Xpressive.Home.ProofOfConcept.Gateways.GoogleCalendar
{
    internal class GoogleCalendarGateway : GatewayBase
    {
        public GoogleCalendarGateway() : base("GoogleCalendar")
        {
        }

        protected override Task ExecuteInternal(DeviceBase device, IAction action, IDictionary<string, string> values)
        {
            throw new NotImplementedException();
        }

        protected override Task SetInternal(DeviceBase device, PropertyBase property, string value)
        {
            throw new NotImplementedException();
        }

        protected override Task<string> GetInternal(DeviceBase device, PropertyBase property)
        {
            return Task.FromResult<string>(null);
        }

        private async Task Observe()
        {
            while (true)
            {
                await Task.Delay(TimeSpan.FromMinutes(1));

                //OnDevicePropertyChanged(_devices.Single(), "property", "value");
            }
        }
    }
}
