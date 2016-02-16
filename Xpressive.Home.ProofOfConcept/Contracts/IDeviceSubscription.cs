using System.Collections.Generic;

namespace Xpressive.Home.ProofOfConcept
{
    public interface IDeviceSubscription
    {
        string GatewayName { get; }
        string DeviceId { get; }

        /// <summary>
        /// All these properties must have the same value to enable the subscription
        /// </summary>
        IDictionary<string, string> DevicePropertyValues { get; }
    }
}