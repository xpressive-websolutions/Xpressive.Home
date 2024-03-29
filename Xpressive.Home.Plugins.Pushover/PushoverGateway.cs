using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Xpressive.Home.Contracts.Gateway;
using Xpressive.Home.Contracts.Messaging;

namespace Xpressive.Home.Plugins.Pushover
{
    internal class PushoverGateway : BackgroundService, IGateway
    {
        private readonly IMessageQueue _messageQueue;
        private readonly IConfiguration _configuration;

        public PushoverGateway(IMessageQueue messageQueue, IConfiguration configuration)
        {
            _messageQueue = messageQueue;
            _configuration = configuration;

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

        public Task<bool> AddDevice(IDevice device)
        {
            throw new NotSupportedException();
        }

        public Task RemoveDevice(IDevice device)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IAction> GetActions(IDevice device)
        {
            yield break;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ContinueWith(_ => { });
            var token = _configuration["pushover.token"];

            if (string.IsNullOrEmpty(token))
            {
                _messageQueue.Publish(new NotifyUserMessage("Add pushover configuration to config file."));
            }
        }
    }
}
