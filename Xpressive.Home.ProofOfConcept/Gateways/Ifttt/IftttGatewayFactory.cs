using System.Collections.Generic;

namespace Xpressive.Home.ProofOfConcept
{
    internal class IftttGatewayFactory : IGatewayDeviceFactory<IftttGateway>
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

            device = ((IftttGateway)gateway).AddDevice(new IftttDevice(apiKey));
            return true;
        }
    }
}