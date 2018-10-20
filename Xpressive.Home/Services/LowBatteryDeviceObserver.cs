using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Xpressive.Home.Contracts.Gateway;
using Xpressive.Home.Contracts.Messaging;

namespace Xpressive.Home.Services
{
    internal sealed class LowBatteryDeviceObserver : BackgroundService
    {
        private readonly IMessageQueue _messageQueue;
        private readonly IList<IGateway> _gateways;

        public LowBatteryDeviceObserver(IMessageQueue messageQueue, IEnumerable<IGateway> gateways)
        {
            _messageQueue = messageQueue;
            _gateways = gateways.ToList();
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                foreach (var gateway in _gateways)
                {
                    foreach (var device in gateway.Devices)
                    {
                        if (device.BatteryStatus == DeviceBatteryStatus.Low)
                        {
                            _messageQueue.Publish(new NotifyUserMessage($"Low battery on device {device.Name} ({gateway.Name}.{device.Id})"));
                        }
                    }
                }

                await Task.Delay(TimeSpan.FromHours(12), cancellationToken).ContinueWith(_ => { });
            }
        }
    }
}
