using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xpressive.Home.Contracts.Messaging;

namespace Xpressive.Home.Contracts.Gateway
{
    public abstract class GatewayBase : IGateway, IMessageQueueListener<CommandMessage>
    {
        private readonly string _name;
        protected readonly IList<DeviceBase> _devices;
        protected readonly IList<IAction> _actions;
        protected bool _canCreateDevices;

        protected GatewayBase(string name)
        {
            _name = name;
            _devices = new List<DeviceBase>();
            _actions = new List<IAction>();
        }

        public string Name => _name;
        public IEnumerable<IDevice> Devices => _devices.ToList();
        public IEnumerable<IAction> Actions => _actions.ToList();
        public bool CanCreateDevices => _canCreateDevices;

        public virtual DeviceBase AddDevice(DeviceBase device)
        {
            if (!_canCreateDevices)
            {
                return null;
            }

            _devices.Add(device);
            return device;
        }

        public virtual async void Notify(CommandMessage message)
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

        protected abstract Task ExecuteInternal(IDevice device, IAction action, IDictionary<string, string> values);
    }
}