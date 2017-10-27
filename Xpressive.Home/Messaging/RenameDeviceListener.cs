using System;
using System.Collections.Generic;
using System.Linq;
using Xpressive.Home.Contracts.Gateway;
using Xpressive.Home.Contracts.Messaging;

namespace Xpressive.Home.Messaging
{
    internal class RenameDeviceListener : IMessageQueueListener<UpdateVariableMessage>
    {
        private readonly IDictionary<string, IGateway> _gateways;

        public RenameDeviceListener(IEnumerable<IGateway> gateways)
        {
            _gateways = gateways.ToDictionary(g => g.Name);
        }

        public void Notify(UpdateVariableMessage message)
        {
            var value = message?.Value as string;

            if (string.IsNullOrEmpty(message?.Name) ||
                string.IsNullOrEmpty(value))
            {
                return;
            }

            var parts = message.Name.Split('.');

            if (parts.Length != 3)
            {
                return;
            }

            if (!parts[2].Equals("Name", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            IGateway gateway;
            if (!_gateways.TryGetValue(parts[0], out gateway))
            {
                return;
            }

            var device = gateway.Devices.SingleOrDefault(d => d.Id.Equals(parts[1], StringComparison.Ordinal));

            if (device == null)
            {
                return;
            }

            device.Name = value;
        }
    }
}
