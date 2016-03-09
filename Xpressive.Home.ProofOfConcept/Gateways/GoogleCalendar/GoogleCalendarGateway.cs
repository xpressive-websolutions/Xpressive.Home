using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xpressive.Home.ProofOfConcept.Contracts;

namespace Xpressive.Home.ProofOfConcept.Gateways.GoogleCalendar
{
    internal class GoogleCalendarGateway : GatewayBase
    {
        public GoogleCalendarGateway() : base("GoogleCalendar")
        {
        }

        public override bool IsConfigurationValid()
        {
            throw new NotImplementedException();
        }

        protected override Task ExecuteInternal(DeviceBase device, IAction action, IDictionary<string, string> values)
        {
            throw new NotImplementedException();
        }

        private async Task Observe()
        {
            while (true)
            {
                await Task.Delay(TimeSpan.FromMinutes(1));
            }
        }
    }
}
