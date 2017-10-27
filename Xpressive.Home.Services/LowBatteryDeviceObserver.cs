using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
        private readonly CancellationTokenSource _cancellationToken = new CancellationTokenSource();

        public LowBatteryDeviceObserver(IMessageQueue messageQueue, IEnumerable<IGateway> gateways)
        {
            _messageQueue = messageQueue;
            _gateways = gateways.ToList();
        }

        public void Start()
        {
            Task.Run(Observe);
        }

        public void Dispose()
        {
            _cancellationToken.Cancel();
            _cancellationToken.Dispose();
        }

        private async Task Observe()
        {
            while (!_cancellationToken.IsCancellationRequested)
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

                await Task.Delay(TimeSpan.FromHours(1), _cancellationToken.Token).ContinueWith(_ => { });
            }
        }
    }
}
