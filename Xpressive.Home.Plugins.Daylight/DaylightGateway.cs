using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xpressive.Home.Contracts.Gateway;
using Xpressive.Home.Contracts.Messaging;

namespace Xpressive.Home.Plugins.Daylight
{
    internal class DaylightGateway : GatewayBase
    {
        private readonly IMessageQueue _messageQueue;

        public DaylightGateway(IMessageQueue messageQueue) : base("Daylight")
        {
            _messageQueue = messageQueue;

            _canCreateDevices = true;

            Observe();
        }

        public override DeviceBase AddDevice(DeviceBase device)
        {
            if (device.IsConfigurationValid())
            {
                return base.AddDevice(device);
            }

            return null;
        }

        protected override Task ExecuteInternal(IDevice device, IAction action, IDictionary<string, string> values)
        {
            throw new NotImplementedException();
        }

        private async Task Observe()
        {
            while (true)
            {
                await Task.Delay(TimeSpan.FromMinutes(1));

                foreach (DaylightDevice device in Devices)
                {
                    var daylight = IsDaylight(device);
                    _messageQueue.Publish(new UpdateVariableMessage(Name, device.Id, "Daylight", daylight));
                }
            }
        }

        private bool IsDaylight(DaylightDevice device)
        {
            var time = DateTime.UtcNow.AddMinutes(device.Offset).TimeOfDay;
            var sunrise = SunsetCalculator.GetSunrise(device.Latitude, device.Longitude);
            var sunset = SunsetCalculator.GetSunset(device.Latitude, device.Longitude);

            return time >= sunrise && time <= sunset;
        }
    }
}
