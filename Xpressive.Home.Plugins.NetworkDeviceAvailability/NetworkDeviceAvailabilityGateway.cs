using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Xpressive.Home.Contracts.Gateway;
using Xpressive.Home.Contracts.Messaging;
using Xpressive.Home.Contracts.Services;

namespace Xpressive.Home.Plugins.NetworkDeviceAvailability
{
    internal sealed class NetworkDeviceAvailabilityGateway : GatewayBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(NetworkDeviceAvailabilityGateway));
        private readonly IMessageQueue _messageQueue;
        private readonly INetworkDeviceService _networkDeviceService;

        public NetworkDeviceAvailabilityGateway(IMessageQueue messageQueue, INetworkDeviceService networkDeviceService) : base("AvailableNetworkDevices")
        {
            _messageQueue = messageQueue;
            _networkDeviceService = networkDeviceService;
            _canCreateDevices = true;
        }

        public override IEnumerable<IAction> GetActions(IDevice device)
        {
            yield break;
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ContinueWith(_ => { }).ConfigureAwait(false);

            while (!cancellationToken.IsCancellationRequested)
            {
                var devices = Devices.OfType<AvailableNetworkDevice>().ToList();

                if (devices.Count > 0)
                {
                    try
                    {
                        var networkDevices = await _networkDeviceService.GetAvailableNetworkDevicesAsync(cancellationToken).ConfigureAwait(false);
                        var macAddresses = networkDevices.Select(d => string.Join(string.Empty, d.MacAddress.Select(b => b.ToString("x2")))).ToList();

                        foreach (var device in devices)
                        {
                            var isAvailable = macAddresses.Contains(device.Id, StringComparer.OrdinalIgnoreCase);
                            _messageQueue.Publish(new UpdateVariableMessage(Name, device.Id, "IsAvailable", isAvailable, "Boolean"));
                        }
                    }
                    catch (Exception e)
                    {
                        _log.Error(e.Message, e);
                    }
                }

                await Task.Delay(TimeSpan.FromMinutes(5), cancellationToken).ContinueWith(_ => { }).ConfigureAwait(false);
            }
        }

        public override IDevice CreateEmptyDevice()
        {
            return new AvailableNetworkDevice
            {
                Id = "00:00:00:00:00:00"
            };
        }

        protected override bool AddDeviceInternal(DeviceBase device)
        {
            if (string.IsNullOrEmpty(device?.Id))
            {
                return false;
            }

            device.Id = device.Id.Replace(":", string.Empty).Replace("-", string.Empty);

            return base.AddDeviceInternal(device);
        }

        protected override Task ExecuteInternalAsync(IDevice device, IAction action, IDictionary<string, string> values)
        {
            throw new NotSupportedException();
        }
    }
}
