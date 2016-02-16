namespace Xpressive.Home.ProofOfConcept
{
    public class DevicePropertyEventArgs
    {
        private readonly string _gatewayName;
        private readonly string _deviceId;
        private readonly string _property;
        private readonly string _value;

        public DevicePropertyEventArgs(string gatewayName, string deviceId, string property, string value)
        {
            _gatewayName = gatewayName;
            _deviceId = deviceId;
            _property = property;
            _value = value;
        }

        public string GatewayName => _gatewayName;
        public string DeviceId => _deviceId;
        public string Property => _property;
        public string Value => _value;
    }
}