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
        protected readonly IList<string> _properties;

        protected GatewayBase(string name)
        {
            _name = name;
            _devices = new List<DeviceBase>();
            _actions = new List<Action>();
            _properties = new List<string>();
        }

        public event EventHandler<DevicePropertyEventArgs> DevicePropertyChanged;

        public IEnumerable<IDevice> Devices => _devices.ToList();
        public IEnumerable<IAction> Actions => _actions.ToList();
        public IEnumerable<string> Properties => _properties.ToList();
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
                if (_properties.Any(p => p.Equals(property, StringComparison.Ordinal)))
                {
                    await GetInternal(deviceBase, property);
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

        public void Execute(IDeviceAction action)
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
                ExecuteInternal(device, actionToExecute, action.ActionFieldValues);
            }
            catch (Exception e)
            {
                // TODO: log
                Console.WriteLine(e);
                device.WriteStatus = DeviceWriteStatus.Erroneous;
            }
        }

        protected virtual void OnDevicePropertyChanged(IDevice device, string property, string value)
        {
            DevicePropertyChanged?.Invoke(this, new DevicePropertyEventArgs(Name, device.Id, property, value));
        }

        protected abstract Task<string> GetInternal(IDevice device, string property);

        protected abstract Task ExecuteInternal(IDevice device, IAction action, IDictionary<string, string> values);

        private DeviceBase GetDevice(IDevice device)
        {
            return _devices.SingleOrDefault(d => d.Equals(device));
        }
    }
}