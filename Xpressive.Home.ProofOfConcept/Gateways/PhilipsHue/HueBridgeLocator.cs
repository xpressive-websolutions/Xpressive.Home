using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using RestSharp;
using RestSharp.Deserializers;

namespace Xpressive.Home.ProofOfConcept.Gateways.PhilipsHue
{
    internal class HueBridgeLocator : IHueBridgeLocator
    {
        private const string _url = "https://www.meethue.com/api/nupnp";
        private readonly IIpAddressService _ipAddressService;

        public HueBridgeLocator(IIpAddressService ipAddressService)
        {
            _ipAddressService = ipAddressService;
        }

        public async Task<IEnumerable<HueBridge>> GetBridgesAsync()
        {
            var bridges = await GetBridgesByNupnpAsync();

            if (bridges == null || bridges.Count <= 0)
            {
                bridges = await GetBridgesByNetworkScanAsync();
            }

            return bridges;
        }

        private async Task<IList<HueBridge>> GetBridgesByNupnpAsync()
        {
            var client = new RestClient(_url);
            client.Timeout = 30000;
            var request = new RestRequest(Method.GET);
            var response = await client.ExecuteTaskAsync<List<HueBridge>>(request);
            return response.Data ?? new List<HueBridge>(0);
        }

        private async Task<IList<HueBridge>> GetBridgesByNetworkScanAsync()
        {
            var addresses = _ipAddressService.GetOtherIpAddresses();
            var bridges = new ConcurrentBag<HueBridge>();

            Parallel.ForEach(addresses, async address =>
            {

                var dto = await GetBridgeXmlDescription(address);

                if (dto != null)
                {
                    bridges.Add(new HueBridge
                    {
                        Id = dto.Device.SerialNumber,
                        InternalIpAddress = address,
                        Name = dto.Device.FriendlyName,
                    });
                }
            });

            return bridges.ToArray();
        }

        private async Task<BridgeXmlDescription> GetBridgeXmlDescription(string ipAddress)
        {
            var url = $"http://{ipAddress}/description.xml";
            var client = new RestClient(url);
            client.Timeout = 5000;
            var request = new RestRequest(Method.GET);
            var response = await client.ExecuteTaskAsync<BridgeXmlDescription>(request);

            if (response.StatusCode == HttpStatusCode.OK && response.Data != null && response.Data.Device != null)
            {
                return response.Data;
            }

            return null;
        }

        [DeserializeAs(Name = "root")]
        public class BridgeXmlDescription
        {
            public DeviceXmlDescription Device { get; set; }
        }

        public class DeviceXmlDescription
        {
            public string SerialNumber { get; set; }
            public string FriendlyName { get; set; }
        }
    }
}