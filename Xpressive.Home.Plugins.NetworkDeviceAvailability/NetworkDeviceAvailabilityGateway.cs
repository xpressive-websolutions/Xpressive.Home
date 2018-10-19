using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using Xpressive.Home.Contracts;
using Xpressive.Home.Contracts.Gateway;
using Xpressive.Home.Contracts.Messaging;

namespace Xpressive.Home.Plugins.NetworkDeviceAvailability
{
    internal sealed class NetworkDeviceAvailabilityGateway : GatewayBase
    {
        private readonly IDictionary<string, DateTime> _lastSeenMacAddresses;
        private readonly IMessageQueue _messageQueue;

        public NetworkDeviceAvailabilityGateway(IMessageQueue messageQueue, IDevicePersistingService persistingService)
            : base("AvailableNetworkDevices", true, persistingService)
        {
            _messageQueue = messageQueue;
            _lastSeenMacAddresses = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);

            _messageQueue.Subscribe<NetworkDeviceFoundMessage>(Notify);
        }

        public override IEnumerable<IAction> GetActions(IDevice device)
        {
            yield break;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ContinueWith(_ => { });

            while (!cancellationToken.IsCancellationRequested)
            {
                var devices = Devices.OfType<AvailableNetworkDevice>().ToList();

                if (devices.Count > 0)
                {
                    try
                    {
                        foreach (var device in devices)
                        {
                            DateTime lastSeen;
                            var id = device.Id.RemoveMacAddressDelimiters();
                            var isAvailable =
                                _lastSeenMacAddresses.TryGetValue(id, out lastSeen) &&
                                DateTime.UtcNow - lastSeen < TimeSpan.FromMinutes(5);

                            _messageQueue.Publish(new UpdateVariableMessage(Name, device.Id, "IsAvailable", isAvailable));
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Error(e, e.Message);
                    }
                }

                await Task.Delay(TimeSpan.FromMinutes(5), cancellationToken).ContinueWith(_ => { });
            }
        }

        public override IDevice CreateEmptyDevice()
        {
            return new AvailableNetworkDevice
            {
                Id = "00:00:00:00:00:00"
            };
        }

        public void Notify(NetworkDeviceFoundMessage message)
        {
            var macAddress = message.MacAddress.MacAddressToString();
            _lastSeenMacAddresses[macAddress] = DateTime.UtcNow;

            AvailableNetworkDevice device;
            if (TryGetDevice(macAddress, out device))
            {
                device.LastSeen = DateTime.UtcNow.ToString("R");
                device.IpAddress = message.IpAddress;
                device.Manufacturer = message.Manufacturer;
            }
        }

        protected override bool AddDeviceInternal(DeviceBase device)
        {
            if (string.IsNullOrEmpty(device?.Id))
            {
                return false;
            }

            device.Id = device.Id.RemoveMacAddressDelimiters();

            return base.AddDeviceInternal(device);
        }

        protected override Task ExecuteInternalAsync(IDevice device, IAction action, IDictionary<string, string> values)
        {
            throw new NotSupportedException();
        }

        private bool TryGetDevice(string id, out AvailableNetworkDevice device)
        {
            device = null;
            if (DeviceDictionary.TryGetValue(id, out var d))
            {
                device = d as AvailableNetworkDevice;
            }
            return device != null;
        }
    }
}
