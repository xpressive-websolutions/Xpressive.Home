using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Xpressive.Home.ProofOfConcept
{
    internal class IftttGateway : GatewayBase
    {
        public IftttGateway() : base("IFTTT")
        {
            _actions.Add(new Action("Web request")
            {
                Fields = { "Event Name" }
            });
        }

        protected override Task<string> GetInternal(IDevice device, string property)
        {
            return Task.FromResult<string>(null);
        }

        protected override async Task ExecuteInternal(IDevice device, IAction action, IDictionary<string, string> values)
        {
            var key = ((IftttDevice)device).Key;
            var eventName = values["Event Name"];
            var url = string.Format("https://maker.ifttt.com/trigger/{1}/with/key/{0}", key, eventName);
            await new HttpClient().PostAsync(url, null);
        }
    }
}