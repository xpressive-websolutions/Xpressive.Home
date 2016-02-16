using System.Collections.Generic;

namespace Xpressive.Home.ProofOfConcept.Gateways.Daylight
{
    internal class DaylightGatewayFactory : IGatewayDeviceFactory<DaylightGateway>
    {
        public IEnumerable<string> GetPropertiesForCreation()
        {
            yield return "Latitude";
            yield return "Longitude";
        }

        public bool TryCreate(IGateway gateway, IDictionary<string, string> properties, out IDevice device)
        {
            device = null;
            string lt;
            string ln;
            double latitude;
            double longitude;

            if (!properties.TryGetValue("Latitude", out lt) || !properties.TryGetValue("Longitude", out ln))
            {
                return false;
            }

            if (!double.TryParse(lt, out latitude) || !double.TryParse(ln, out longitude))
            {
                return false;
            }

            device = ((DaylightGateway)gateway).AddDevice(new DaylightDevice(latitude, longitude));
            return true;
        }
    }
}