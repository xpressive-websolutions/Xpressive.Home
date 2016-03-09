using System.Collections.Generic;

namespace Xpressive.Home.ProofOfConcept.Contracts
{
    public interface IGatewayResolver
    {
        void Register(IGateway gateway);

        IGateway Resolve(string gatewayName);

        IEnumerable<IGateway> GetAll();
    }
}