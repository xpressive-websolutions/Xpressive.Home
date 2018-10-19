using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Xpressive.Home.Contracts.Gateway;
using Xpressive.Home.Contracts.Messaging;

namespace Xpressive.Home.Plugins.Sms
{
    internal sealed class SmsGateway : BackgroundService, IGateway
    {
        private readonly IMessageQueue _messageQueue;
        private readonly IConfiguration _configuration;

        public SmsGateway(IMessageQueue messageQueue, IConfiguration configuration)
        {
            _messageQueue = messageQueue;
            _configuration = configuration;

            Name = "SMS";
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

        public void RemoveDevice(IDevice device)
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
            var username = _configuration["sms.username"];
            var password = _configuration["sms.password"];

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                _messageQueue.Publish(new NotifyUserMessage("Add sms configuration to config file."));
            }
        }

        public void Dispose() { }
    }
}
