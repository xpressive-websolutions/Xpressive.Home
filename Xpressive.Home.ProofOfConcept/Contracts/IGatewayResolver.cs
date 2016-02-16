using System.Collections.Generic;

namespace Xpressive.Home.ProofOfConcept
{
    public interface IGatewayResolver
    {
        void Register(IGateway gateway);

        IGateway Resolve(string gatewayName);

        IEnumerable<IGateway> GetAll();
    }
}