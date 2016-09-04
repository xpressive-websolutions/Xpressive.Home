using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xpressive.Home.Contracts;
using Xpressive.Home.Contracts.Gateway;
using Xpressive.Home.Contracts.Messaging;

namespace Xpressive.Home.Plugins.Daylight
{
    internal class DaylightGateway : GatewayBase, IDaylightGateway
    {
        private readonly IMessageQueue _messageQueue;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(0);
        private bool _isRunning = true;

        public DaylightGateway(IMessageQueue messageQueue) : base("Daylight")
        {
            _messageQueue = messageQueue;

            _canCreateDevices = true;
        }

        public override IDevice CreateEmptyDevice()
        {
            return new DaylightDevice();
        }

        public IEnumerable<DaylightDevice> GetDevices()
        {
            return Devices.OfType<DaylightDevice>();
        }

        public override async Task StartAsync()
        {
            await Task.Delay(TimeSpan.FromSeconds(1));

            await LoadDevicesAsync((id, name) => new DaylightDevice { Id = id, Name = name });

            while (_isRunning)
            {
                foreach (var device in GetDevices())
                {
                    UpdateVariables(device);
                }

                await TaskHelper.DelayAsync(TimeSpan.FromMinutes(1), () => _isRunning);
            }

            _semaphore.Release();
        }

        public override void Stop()
        {
            _isRunning = false;
            _semaphore.Wait(TimeSpan.FromSeconds(5));
        }

        protected override Task ExecuteInternal(IDevice device, IAction action, IDictionary<string, string> values)
        {
            throw new NotImplementedException();
        }

        protected override void Dispose(bool disposing)
        {
            _isRunning = false;
            base.Dispose(disposing);
        }

        private void UpdateVariables(DaylightDevice device)
        {
            var time = DateTime.UtcNow.AddMinutes(device.OffsetInMinutes).TimeOfDay;
            var sunrise = SunsetCalculator.GetSunrise(device.Latitude, device.Longitude);
            var sunset = SunsetCalculator.GetSunset(device.Latitude, device.Longitude);

            device.IsDaylight = time >= sunrise && time <= sunset;
            _messageQueue.Publish(new UpdateVariableMessage(Name, device.Id, "IsDaylight", device.IsDaylight));

            var offset = DateTime.UtcNow - DateTime.Now;
            _messageQueue.Publish(new UpdateVariableMessage(Name, device.Id, "Sunrise", (sunrise - offset).ToString("hh\\:mm\\:ss")));
            _messageQueue.Publish(new UpdateVariableMessage(Name, device.Id, "Sunset", (sunset - offset).ToString("hh\\:mm\\:ss")));
        }
    }
}
