using System.Collections.Generic;

namespace Xpressive.Home.ProofOfConcept
{
    public interface IDeviceAction
    {
        string GatewayName { get; }
        string DeviceId { get; }
        string ActionName { get; }

        IDictionary<string, string> ActionFieldValues { get; }
    }
}