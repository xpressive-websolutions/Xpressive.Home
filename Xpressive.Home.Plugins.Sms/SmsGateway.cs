using System;
using System.Collections.Generic;
using System.Configuration;
using System.Threading.Tasks;
using Xpressive.Home.Contracts.Gateway;
using Xpressive.Home.Contracts.Messaging;

namespace Xpressive.Home.Plugins.Sms
{
    internal sealed class SmsGateway : IGateway
    {
        private readonly IMessageQueue _messageQueue;

        public SmsGateway(IMessageQueue messageQueue)
        {
            _messageQueue = messageQueue;

            Name = "SMS";
            CanCreateDevices = false;
            Devices = new List<IDevice>(0);
            Actions = new List<IAction>(0);
        }

        public string Name { get; }
        public bool CanCreateDevices { get; }
        public IEnumerable<IDevice> Devices { get; }
        public IEnumerable<IAction> Actions { get; }

        public IDevice CreateEmptyDevice()
        {
            throw new NotSupportedException();
        }

        public bool AddDevice(IDevice device)
        {
            throw new NotSupportedException();
        }

        public async Task StartAsync()
        {
            await Task.Delay(TimeSpan.FromSeconds(1));
            var username = ConfigurationManager.AppSettings["sms.username"];
            var password = ConfigurationManager.AppSettings["sms.password"];

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                _messageQueue.Publish(new NotifyUserMessage("Add sms configuration to config file."));
            }
        }

        public void Stop() { }

        public void Dispose() { }
    }
}
