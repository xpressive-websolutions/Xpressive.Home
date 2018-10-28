using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Serilog;
using Xpressive.Home.Contracts.Messaging;

namespace Xpressive.Home.Contracts.Gateway
{
    public abstract class GatewayBase : BackgroundService, IGateway
    {
        protected GatewayBase(IMessageQueue messageQueue, string name, bool canCreateDevices, IDevicePersistingService devicePersistingService = null)
        {
            MessageQueue = messageQueue;
            Name = name;
            CanCreateDevices = canCreateDevices;
            PersistingService = devicePersistingService;
            DeviceDictionary = new ConcurrentDictionary<string, DeviceBase>(StringComparer.Ordinal);

            MessageQueue.Subscribe<CommandMessage>(Notify);
            MessageQueue.Subscribe<RenameDeviceMessage>(Notify);
        }

        public string Name { get; }
        public IEnumerable<IDevice> Devices => DeviceDictionary.Values.ToList();
        public bool CanCreateDevices { get; }
        public IDevicePersistingService PersistingService { get; }
        public IMessageQueue MessageQueue { get; }
        protected ConcurrentDictionary<string, DeviceBase> DeviceDictionary { get; }

        public async Task<bool> AddDevice(IDevice device)
        {
            var d = device as DeviceBase;
            return await AddDeviceInternal(d);
        }

        public async Task RemoveDevice(IDevice device)
        {
            if (!CanCreateDevices)
            {
                throw new InvalidOperationException("Unable to remove devices.");
            }

            if (DeviceDictionary.TryRemove(device.Id, out DeviceBase d))
            {
                await PersistingService.DeleteAsync(Name, d);
            }
        }

        public abstract IEnumerable<IAction> GetActions(IDevice device);
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

            if (!DeviceDictionary.TryGetValue(deviceId, out var device))
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

        private void Notify(RenameDeviceMessage message)
        {
            if (!Name.Equals(message.Gateway, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (!DeviceDictionary.TryGetValue(message.Device, out var device))
            {
                return;
            }

            device.Name = message.Name;
        }

        protected async Task LoadDevicesAsync(Func<string, string, DeviceBase> emptyDevice)
        {
            try
            {
                var devices = await PersistingService.GetAsync(Name, emptyDevice);

                foreach (var device in devices)
                {
                    DeviceDictionary.AddOrUpdate(device.Id, device, (_, e) => device);
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

        protected virtual async Task<bool> AddDeviceInternal(DeviceBase device)
        {
            if (!CanCreateDevices || device == null || !device.IsConfigurationValid())
            {
                return false;
            }

            DeviceDictionary.AddOrUpdate(device.Id, device, (_, e) => device);
            await PersistingService.SaveAsync(Name, device);
            return true;
        }

        protected abstract Task ExecuteInternalAsync(IDevice device, IAction action, IDictionary<string, string> values);

        public override void Dispose()
        {
            Dispose(true);
            base.Dispose();
        }

        protected virtual void Dispose(bool disposing) { }

        ~GatewayBase()
        {
            Dispose(false);
        }
    }
}
