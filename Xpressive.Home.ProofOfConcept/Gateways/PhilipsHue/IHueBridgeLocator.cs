using System.Collections.Generic;
using System.Threading.Tasks;

namespace Xpressive.Home.ProofOfConcept.Gateways.PhilipsHue
{
    internal interface IHueBridgeLocator
    {
        Task<IEnumerable<HueBridge>> GetBridgesAsync();
    }
}