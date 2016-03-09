using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Threading.Tasks;
using Xpressive.Home.ProofOfConcept.Contracts;

namespace Xpressive.Home.ProofOfConcept.Gateways.Pushalot
{
    internal class PushalotGateway : GatewayBase
    {
        public PushalotGateway() : base("Pushalot")
        {
        }

        public override bool IsConfigurationValid()
        {
            throw new NotImplementedException();
        }

        protected override async Task ExecuteInternal(DeviceBase device, IAction action, IDictionary<string, string> values)
        {
            var key = ((PushalotDevice)device).Key;
            var text = values["Text"];

            using (var client = new WebClient())
            {
                var data = new NameValueCollection();
                data["AuthorizationToken"] = key;
                data["Body"] = text;
                await client.UploadValuesTaskAsync("https://pushalot.com/api/sendmessage", data);
            }
        }
    }
}