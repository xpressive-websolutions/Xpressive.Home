using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xpressive.Home.ProofOfConcept.Contracts;

namespace Xpressive.Home.ProofOfConcept.Gateways.Netatmo
{
    internal class NetatmoGateway : GatewayBase
    {
        public NetatmoGateway() : base("Netatmo")
        {
            ClientId = "";
            ClientSecret = "";

            Setup();
        }

        [GatewayProperty]
        public string ClientId { get; set; }

        [GatewayProperty]
        public string ClientSecret { get; set; }

        public override bool IsConfigurationValid()
        {
            if (string.IsNullOrEmpty(ClientId) || string.IsNullOrEmpty(ClientSecret))
            {
                return false;
            }

            // check connection to the cloud
            return true;
        }

        protected override Task ExecuteInternal(DeviceBase device, IAction action, IDictionary<string, string> values)
        {
            throw new NotImplementedException();
        }

        private async Task Setup()
        {
            while (!IsConfigurationValid())
            {
                await Task.Delay(1000);
            }

            // find devices
        }
    }

    internal class NetatmoDevice : DeviceBase
    {
        public NetatmoDevice(string id, string name)
        {
            Id = id;
            Name = name;
        }
    }
}
