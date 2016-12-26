using System;
using System.Collections.Generic;
using System.Configuration;
using System.Threading;
using System.Threading.Tasks;
using Xpressive.Home.Contracts.Gateway;
using Xpressive.Home.Contracts.Messaging;

namespace Xpressive.Home.Plugins.Pushover
{
    internal class PushoverGateway : IGateway
    {
        private readonly IMessageQueue _messageQueue;

        public PushoverGateway(IMessageQueue messageQueue)
        {
            _messageQueue = messageQueue;

            Name = "Pushover";
            CanCreateDevices = false;
            Devices = new List<IDevice>(0);
        }

        public string Name { get; }
        public bool CanCreateDevices { get; }
        public IEnumerable<IDevice> Devices { get; }

        public IDevice CreateEmptyDevice()
        {
            throw new NotSupportedException();
        }

        public bool AddDevice(IDevice device)
        {
            throw new NotSupportedException();
        }

        public IEnumerable<IAction> GetActions(IDevice device)
        {
            yield break;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ContinueWith(_ => { });
            var token = ConfigurationManager.AppSettings["pushover.token"];

            if (string.IsNullOrEmpty(token))
            {
                _messageQueue.Publish(new NotifyUserMessage("Add pushover configuration to config file."));
            }
        }

        public void Dispose() { }
    }
}
