using System.Collections.Generic;

namespace Xpressive.Home.ProofOfConcept.Gateways.Pushalot
{
    internal class PushalotGatewayFactory : IGatewayDeviceFactory<PushalotGateway>
    {
        public IEnumerable<string> GetPropertiesForCreation()
        {
            yield return "Api Key";
        }

        public bool TryCreate(IGateway gateway, IDictionary<string, string> properties, out IDevice device)
        {
            device = null;
            string apiKey;

            if (!properties.TryGetValue("Api Key", out apiKey))
            {
                return false;
            }

            device = ((PushalotGateway)gateway).AddDevice(new PushalotDevice(apiKey));
            return true;
        }
    }
}