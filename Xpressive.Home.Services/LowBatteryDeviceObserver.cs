using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Xpressive.Home.Contracts.Gateway;
using Xpressive.Home.Contracts.Messaging;

namespace Xpressive.Home.Services
{
    internal sealed class LowBatteryDeviceObserver : IStartable, IDisposable
    {
        private readonly IMessageQueue _messageQueue;
        private readonly IList<IGateway> _gateways;
        private bool _isRunning;

        public LowBatteryDeviceObserver(IMessageQueue messageQueue, IEnumerable<IGateway> gateways)
        {
            _messageQueue = messageQueue;
            _gateways = gateways.ToList();
        }

        public void Start()
        {
            _isRunning = true;
            Task.Run(Observe);
        }

        public void Dispose()
        {
            _isRunning = false;
        }

        private async Task Observe()
        {
            var lastRun = DateTime.MinValue;

            while (_isRunning)
            {
                while (_isRunning && ((DateTime.UtcNow - lastRun) < TimeSpan.FromMinutes(1)))
                {
                    await Task.Delay(10);
                }

                lastRun = DateTime.UtcNow;

                foreach (var gateway in _gateways)
                {
                    foreach (var device in gateway.Devices)
                    {
                        if (device.BatteryStatus == DeviceBatteryStatus.Low)
                        {
                            _messageQueue.Publish(new LowBatteryMessage(gateway.Name, device));
                        }
                    }
                }
            }
        }
    }
}
