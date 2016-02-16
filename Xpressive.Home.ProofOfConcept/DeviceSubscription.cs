using System.Collections.Generic;

namespace Xpressive.Home.ProofOfConcept
{
    internal class DeviceSubscription : IDeviceSubscription
    {
        private readonly string _gatewayName;
        private readonly IDictionary<string, string> _devicePropertyValues;
        private readonly string _deviceId;

        public DeviceSubscription(string gatewayName, string deviceId, IDictionary<string, string> values)
        {
            _gatewayName = gatewayName;
            _deviceId = deviceId;
            _devicePropertyValues = new Dictionary<string, string>(values);
        }

        public string DeviceId => _deviceId;
        public IDictionary<string, string> DevicePropertyValues => _devicePropertyValues;
        public string GatewayName => _gatewayName;
    }
}