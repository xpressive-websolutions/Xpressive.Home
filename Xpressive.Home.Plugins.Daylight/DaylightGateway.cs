using System;
using System.Collections.Generic;
using System.Linq;
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
        }

        public override IDevice CreateEmptyDevice()
        {
            return new DaylightDevice();
        }

        public async Task StartObservationAsync()
        {
            await Task.Delay(TimeSpan.FromSeconds(5));

            await LoadDevicesAsync((id, name) => new DaylightDevice { Id = id, Name = name });

            while (true)
            {
                await Task.Delay(TimeSpan.FromMinutes(1));

                foreach (var device in Devices.Cast<DaylightDevice>())
                {
                    var daylight = IsDaylight(device);
                    _messageQueue.Publish(new UpdateVariableMessage(Name, device.Id, "IsDaylight", daylight));
                }
            }
        }

        protected override Task ExecuteInternal(IDevice device, IAction action, IDictionary<string, string> values)
        {
            throw new NotImplementedException();
        }

        private bool IsDaylight(DaylightDevice device)
        {
            var time = DateTime.UtcNow.AddMinutes(device.OffsetInMinutes).TimeOfDay;
            var sunrise = SunsetCalculator.GetSunrise(device.Latitude, device.Longitude);
            var sunset = SunsetCalculator.GetSunset(device.Latitude, device.Longitude);

            return time >= sunrise && time <= sunset;
        }
    }
}
