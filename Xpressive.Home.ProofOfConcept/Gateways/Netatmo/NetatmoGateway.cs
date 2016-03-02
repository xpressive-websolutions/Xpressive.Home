using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xpressive.Home.ProofOfConcept.Gateways.Netatmo
{
    internal class NetatmoGateway : GatewayBase
    {
        private const string _clientId = "";
        private const string _clientSecret = "";

        public NetatmoGateway() : base("Netatmo")
        {
            
        }

        protected override Task ExecuteInternal(DeviceBase device, IAction action, IDictionary<string, string> values)
        {
            throw new NotImplementedException();
        }

        protected override Task<string> GetInternal(DeviceBase device, PropertyBase property)
        {
            throw new NotImplementedException();
        }

        protected override Task SetInternal(DeviceBase device, PropertyBase property, string value)
        {
            throw new NotImplementedException();
        }
    }

    internal class NetatmoDevice : DeviceBase
    {
        public NetatmoDevice() : base("", "")
        {
            
        }
    }
}
