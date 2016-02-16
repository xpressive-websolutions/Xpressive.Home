using System;
using System.Collections.Generic;

namespace Xpressive.Home.ProofOfConcept
{
    public class DeviceAction : IDeviceAction
    {
        private readonly string _gatewayName;
        private readonly string _deviceId;
        private readonly string _actionName;
        private readonly Dictionary<string, string> _values;

        public DeviceAction(string gatewayName, string deviceId, string actionName)
        {
            _gatewayName = gatewayName;
            _deviceId = deviceId;
            _actionName = actionName;
            _values = new Dictionary<string, string>(StringComparer.Ordinal);
        }

        public string GatewayName => _gatewayName;
        public string DeviceId => _deviceId;
        public string ActionName => _actionName;
        public IDictionary<string, string> ActionFieldValues => _values;
    }
}