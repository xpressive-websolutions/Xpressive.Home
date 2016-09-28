using System;
using System.Collections.Generic;
using System.Configuration;
using System.Threading.Tasks;
using Xpressive.Home.Contracts.Gateway;
using Xpressive.Home.Contracts.Messaging;

namespace Xpressive.Home.Plugins.Pushalot
{
    internal class PushalotGateway : IGateway
    {
        private readonly IMessageQueue _messageQueue;

        public PushalotGateway(IMessageQueue messageQueue)
        {
            _messageQueue = messageQueue;

            Name = "Pushalot";
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

        public async Task StartAsync()
        {
            await Task.Delay(TimeSpan.FromSeconds(1));
            var token = ConfigurationManager.AppSettings["pushalot.token"];

            if (string.IsNullOrEmpty(token))
            {
                _messageQueue.Publish(new NotifyUserMessage("Add pushalot configuration to config file."));
            }
        }

        public void Stop() { }

        public void Dispose() { }
    }
}
