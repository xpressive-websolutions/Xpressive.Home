using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Xpressive.Home.ProofOfConcept
{
    internal abstract class GatewayBase : IGateway
    {
        private readonly string _name;
        protected readonly IList<DeviceBase> _devices;
        protected readonly IList<Action> _actions;
        protected readonly IList<PropertyBase> _properties;

        protected GatewayBase(string name)
        {
            _name = name;
            _devices = new List<DeviceBase>();
            _actions = new List<Action>();
            _properties = new List<PropertyBase>();
        }

        public event EventHandler<DevicePropertyEventArgs> DevicePropertyChanged;

        public IEnumerable<IDevice> Devices => _devices.ToList();
        public IEnumerable<IAction> Actions => _actions.ToList();
        public IEnumerable<IProperty> Properties => _properties.ToList();
        public string Name => _name;

        public DeviceBase AddDevice(DeviceBase device)
        {
            _devices.Add(device);
            return device;
        }

        public async Task<string> Get(IDevice device, string property)
        {
            var deviceBase = GetDevice(device);

            try
            {
                var prop = _properties.SingleOrDefault(p => p.Name.Equals(property, StringComparison.Ordinal));
                if (deviceBase != null && prop != null)
                {
                    await GetInternal(deviceBase, prop);
                    deviceBase.ReadStatus = DeviceReadStatus.Ok;
                }
            }
            catch (Exception e)
            {
                // TODO: log
                Console.WriteLine(e);
                deviceBase.ReadStatus = DeviceReadStatus.Erroneous;
            }

            return null;
        }

        public async Task Set(IDevice device, string property, string value)
        {
            var deviceBase = GetDevice(device);

            try
            {
                var prop = _properties.SingleOrDefault(p => p.Name.Equals(property, StringComparison.Ordinal));
                if (deviceBase != null && prop != null && !prop.IsReadOnly && prop.IsValidValue(value))
                {
                    Console.WriteLine($"Set {property} of {device.Name} to {value}");
                    await SetInternal(deviceBase, prop, value);
                    deviceBase.WriteStatus = DeviceWriteStatus.Ok;
                }
            }
            catch (Exception e)
            {
                // TODO: log
                Console.WriteLine(e);
                deviceBase.WriteStatus = DeviceWriteStatus.Erroneous;
            }
        }

        public async Task Execute(IDeviceAction action)
        {
            if (!_name.Equals(action.GatewayName, StringComparison.Ordinal))
            {
                return;
            }

            var device = _devices.SingleOrDefault(d => d.Id.Equals(action.DeviceId));

            if (device == null)
            {
                return;
            }

            var actionToExecute = _actions.SingleOrDefault(a => a.Name.Equals(action.ActionName, StringComparison.Ordinal));

            if (actionToExecute == null)
            {
                return;
            }

            try
            {
                await ExecuteInternal(device, actionToExecute, action.ActionFieldValues);
            }
            catch (Exception e)
            {
                // TODO: log
                Console.WriteLine(e);
                device.WriteStatus = DeviceWriteStatus.Erroneous;
            }
        }

        protected virtual void OnDevicePropertyChanged(DeviceBase device, PropertyBase property, string value)
        {
            DevicePropertyChanged?.Invoke(this, new DevicePropertyEventArgs(Name, device.Id, property.Name, value));
        }

        protected PropertyBase GetProperty(string property)
        {
            return _properties.Single(p => p.Name.Equals(property, StringComparison.Ordinal));
        }

        protected abstract Task<string> GetInternal(DeviceBase device, PropertyBase property);

        protected abstract Task SetInternal(DeviceBase device, PropertyBase property, string value);

        protected abstract Task ExecuteInternal(DeviceBase device, IAction action, IDictionary<string, string> values);

        private DeviceBase GetDevice(IDevice device)
        {
            return _devices.SingleOrDefault(d => d.Equals(device));
        }
    }
}