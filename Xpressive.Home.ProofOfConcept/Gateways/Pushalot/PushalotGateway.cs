using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Threading.Tasks;

namespace Xpressive.Home.ProofOfConcept.Gateways.Pushalot
{
    internal class PushalotGateway : GatewayBase
    {
        public PushalotGateway() : base("Pushalot")
        {
            _actions.Add(new Action("Send Notification")
            {
                Fields = { "Text" }
            });
        }

        protected override Task<string> GetInternal(DeviceBase device, PropertyBase property)
        {
            return Task.FromResult<string>(null);
        }

        protected override Task SetInternal(DeviceBase device, PropertyBase property, string value)
        {
            throw new System.NotImplementedException();
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