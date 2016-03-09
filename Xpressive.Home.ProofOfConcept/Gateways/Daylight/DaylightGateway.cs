using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Xpressive.Home.ProofOfConcept.Gateways.Daylight
{
    internal class DaylightGateway : GatewayBase
    {
        private readonly IVariableRepository _variableRepository;
        private readonly IMessageQueue _messageQueue;

        public DaylightGateway(IVariableRepository variableRepository, IMessageQueue messageQueue) : base("Daylight")
        {
            _variableRepository = variableRepository;
            _messageQueue = messageQueue;
            _properties.Add(new BoolProperty("Daylight", isReadOnly: true));

            CanCreateDevices = true;

            Observe();
        }

        public override bool IsConfigurationValid()
        {
            return true;
        }

        public override DeviceBase AddDevice(DeviceBase device)
        {
            if (device.IsConfigurationValid())
            {
                _variableRepository.Register(new BooleanVariable($"{Name}.{device.Name}.Daylight"));
                return base.AddDevice(device);
            }

            return null;
        }

        protected override Task ExecuteInternal(DeviceBase device, IAction action, IDictionary<string, string> values)
        {
            return Task.CompletedTask;
        }

        private async Task Observe()
        {
            while (true)
            {
                await Task.Delay(TimeSpan.FromMinutes(1));

                foreach (DaylightDevice device in Devices)
                {
                    var daylight = IsDaylight(device);
                    _messageQueue.Publish(new UpdateVariableMessage(Name, device.Name, "Daylight", daylight));
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