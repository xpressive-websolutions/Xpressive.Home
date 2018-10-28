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

        public NetworkDeviceAvailabilityGateway(IMessageQueue messageQueue, IDevicePersistingService persistingService)
            : base(messageQueue, "AvailableNetworkDevices", true, persistingService)
        {
            _lastSeenMacAddresses = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);

            messageQueue.Subscribe<NetworkDeviceFoundMessage>(Notify);
        }

        public override IEnumerable<IAction> GetActions(IDevice device)
        {
            yield break;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ContinueWith(_ => { });

            await LoadDevicesAsync((id, name) => new AvailableNetworkDevice { Id = id, Name = name });

            while (!cancellationToken.IsCancellationRequested)
            {
                var devices = Devices.OfType<AvailableNetworkDevice>().ToList();

                if (devices.Count > 0)
                {
                    try
                    {
                        foreach (var device in devices)
                        {
                            var id = device.Id.RemoveMacAddressDelimiters();
                            var isAvailable =
                                _lastSeenMacAddresses.TryGetValue(id, out var lastSeen) &&
                                DateTime.UtcNow - lastSeen < TimeSpan.FromMinutes(5);

                            if (device.IsAvailable != isAvailable)
                            {
                                device.IsAvailable = isAvailable;
                                MessageQueue.Publish(new UpdateVariableMessage(Name, device.Id, "IsAvailable", isAvailable));
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Error(e, e.Message);
                    }
                }

                await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken).ContinueWith(_ => { });
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

            if (TryGetDevice(macAddress, out AvailableNetworkDevice device))
            {
                device.LastSeen = DateTime.UtcNow.ToString("R");
                device.IpAddress = message.IpAddress;
                device.Manufacturer = message.Manufacturer;
            }
        }

        protected override async Task<bool> AddDeviceInternal(DeviceBase device)
        {
            if (string.IsNullOrEmpty(device?.Id))
            {
                return false;
            }

            device.Id = device.Id.RemoveMacAddressDelimiters();

            return await base.AddDeviceInternal(device);
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
