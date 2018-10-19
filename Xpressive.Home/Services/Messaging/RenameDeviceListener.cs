using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Xpressive.Home.Contracts.Gateway;
using Xpressive.Home.Contracts.Messaging;

namespace Xpressive.Home.Services.Messaging
{
    internal class RenameDeviceListener : BackgroundService
    {
        private readonly IDictionary<string, IGateway> _gateways;

        public RenameDeviceListener(IEnumerable<IGateway> gateways, IMessageQueue messageQueue)
        {
            _gateways = gateways.ToDictionary(g => g.Name);

            messageQueue.Subscribe<UpdateVariableMessage>(Notify);
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

        protected override Task ExecuteAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
