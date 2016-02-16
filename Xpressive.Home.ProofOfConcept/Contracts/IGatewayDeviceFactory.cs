using System.Collections.Generic;

namespace Xpressive.Home.ProofOfConcept
{
    public interface IGatewayDeviceFactory<T> where T : IGateway
    {
        bool TryCreate(IGateway gateway, IDictionary<string, string> properties, out IDevice device);

        IEnumerable<string> GetPropertiesForCreation();
    }
}