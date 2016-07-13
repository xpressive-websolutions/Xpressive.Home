using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using log4net;
using Xpressive.Home.Contracts.Messaging;

namespace Xpressive.Home.Contracts.Gateway
{
    public abstract class GatewayBase : IGateway, IMessageQueueListener<CommandMessage>
    {
        private readonly ILog _log;
        private readonly string _name;
        protected readonly IList<DeviceBase> _devices;
        protected readonly IList<IAction> _actions;
        protected bool _canCreateDevices;

        protected GatewayBase(string name)
        {
            _log = LogManager.GetLogger(GetType());
            _name = name;
            _devices = new List<DeviceBase>();
            _actions = new List<IAction>();
        }

        public string Name => _name;
        public IEnumerable<IDevice> Devices => _devices.ToList();
        public IEnumerable<IAction> Actions => _actions.ToList();
        public bool CanCreateDevices => _canCreateDevices;
        
        public IDevicePersistingService PersistingService { get; set; }

        public bool AddDevice(IDevice device)
        {
            var d = device as DeviceBase;
            return AddDeviceInternal(d);
        }

        public async void Notify(CommandMessage message)
        {
            if (!message.ActionId.StartsWith(_name, StringComparison.Ordinal))
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
            var device = Devices.SingleOrDefault(d => d.Id.Equals(deviceId, StringComparison.Ordinal));
            var action = Actions.SingleOrDefault(a => a.Name.Equals(actionName, StringComparison.Ordinal));

            if (device == null || action == null)
            {
                return;
            }

            await ExecuteInternal(device, action, message.Parameters);
        }

        protected async Task LoadDevicesAsync(Func<string, string, DeviceBase> emptyDevice)
        {
            try
            {
                var devices = await PersistingService.GetAsync(Name, emptyDevice);

                foreach (var device in devices)
                {
                    _devices.Add(device);
                }
            }
            catch (Exception e)
            {
                _log.Error("Unable to load persisted devices.", e);
            }
        }

        private bool AddDeviceInternal(DeviceBase device)
        {
            if (!_canCreateDevices || device == null || !device.IsConfigurationValid())
            {
                return false;
            }

            _devices.Add(device);
            PersistingService.SaveAsync(Name, device);
            return true;
        }

        public abstract IDevice CreateEmptyDevice();

        protected abstract Task ExecuteInternal(IDevice device, IAction action, IDictionary<string, string> values);

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