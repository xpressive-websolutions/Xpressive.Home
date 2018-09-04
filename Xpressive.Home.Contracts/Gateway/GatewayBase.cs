using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xpressive.Home.Contracts.Messaging;

namespace Xpressive.Home.Contracts.Gateway
{
    public abstract class GatewayBase : IGateway, IMessageQueueListener<CommandMessage>
    {
        protected readonly ConcurrentDictionary<string, DeviceBase> _devices;
        protected bool _canCreateDevices;

        protected GatewayBase(string name)
        {
            Name = name;
            _devices = new ConcurrentDictionary<string, DeviceBase>(StringComparer.Ordinal);
        }

        public string Name { get; }
        public IEnumerable<IDevice> Devices => _devices.Values.ToList();
        public bool CanCreateDevices => _canCreateDevices;

        public IDevicePersistingService PersistingService { get; set; }

        public bool AddDevice(IDevice device)
        {
            var d = device as DeviceBase;
            return AddDeviceInternal(d);
        }

        public void RemoveDevice(IDevice device)
        {
            if (!_canCreateDevices)
            {
                throw new InvalidOperationException("Unable to remove devices.");
            }

            DeviceBase d;
            if (_devices.TryRemove(device.Id, out d))
            {
                PersistingService.DeleteAsync(Name, d);
            }
        }

        public abstract IEnumerable<IAction> GetActions(IDevice device);

        public abstract Task StartAsync(CancellationToken cancellationToken);
        public abstract IDevice CreateEmptyDevice();

        public void Notify(CommandMessage message)
        {
            if (!message.ActionId.StartsWith(Name, StringComparison.Ordinal))
            {
                return;
            }

            var parts = message.ActionId.Split('.');

            if (parts.Length != 3)
            {
                return;
            }

            var deviceId = parts[1];
            var actionName = parts[2];
            DeviceBase device;
            if (!_devices.TryGetValue(deviceId, out device))
            {
                return;
            }

            var action = GetActions(device).SingleOrDefault(a => a.Name.Equals(actionName, StringComparison.Ordinal));

            if (action == null)
            {
                return;
            }

            StartActionInNewTask(device, action, message.Parameters);
        }

        protected async Task LoadDevicesAsync(Func<string, string, DeviceBase> emptyDevice)
        {
            try
            {
                var devices = await PersistingService.GetAsync(Name, emptyDevice);

                foreach (var device in devices)
                {
                    _devices.AddOrUpdate(device.Id, device, (_, e) => device);
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "Unable to load persisted devices.");
            }
        }

        protected void StartActionInNewTask(IDevice device, IAction action, IDictionary<string, string> values)
        {
            Task.Factory.StartNew(async () => await ExecuteInternalAsync(device, action, values));
        }

        protected virtual bool AddDeviceInternal(DeviceBase device)
        {
            if (!_canCreateDevices || device == null || !device.IsConfigurationValid())
            {
                return false;
            }

            _devices.AddOrUpdate(device.Id, device, (_, e) => device);
            PersistingService.SaveAsync(Name, device);
            return true;
        }

        protected abstract Task ExecuteInternalAsync(IDevice device, IAction action, IDictionary<string, string> values);

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing) { }

        ~GatewayBase()
        {
            Dispose(false);
        }
    }
}
