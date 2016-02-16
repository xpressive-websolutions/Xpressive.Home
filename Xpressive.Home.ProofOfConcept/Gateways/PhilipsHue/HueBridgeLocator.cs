using System.Collections.Generic;
using System.Threading.Tasks;
using RestSharp;

namespace Xpressive.Home.ProofOfConcept.Gateways.PhilipsHue
{
    internal class HueBridgeLocator : IHueBridgeLocator
    {
        private const string _url = "https://www.meethue.com/api/nupnp";

        public async Task<IEnumerable<HueBridge>> GetBridgesAsync()
        {
            var client = new RestClient(_url);
            client.Timeout = 60000;
            var request = new RestRequest(Method.GET);
            var response = await client.ExecuteTaskAsync<HueBridge[]>(request);
            return response.Data ?? new HueBridge[0];
        }
    }
}