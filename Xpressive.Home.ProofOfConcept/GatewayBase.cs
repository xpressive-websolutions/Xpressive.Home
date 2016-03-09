using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Xpressive.Home.ProofOfConcept
{
    internal abstract class GatewayBase : IGateway
    {
        private readonly string _name;
        private bool _canCreateDevices;
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

        public IEnumerable<IDevice> Devices => _devices.ToList();
        public IEnumerable<IAction> Actions => _actions.ToList();
        public IEnumerable<IProperty> Properties => _properties.ToList();
        public string Name => _name;
        public bool CanCreateDevices { get; protected set; }

        public abstract bool IsConfigurationValid();

        public virtual DeviceBase AddDevice(DeviceBase device)
        {
            _devices.Add(device);
            return device;
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

        protected PropertyBase GetProperty(string property)
        {
            return _properties.Single(p => p.Name.Equals(property, StringComparison.Ordinal));
        }

        protected abstract Task ExecuteInternal(DeviceBase device, IAction action, IDictionary<string, string> values);
    }
}