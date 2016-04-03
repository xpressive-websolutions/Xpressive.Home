using Xpressive.Home.Contracts.Gateway;

namespace Xpressive.Home.Contracts.Messaging
{
    public sealed class LowBatteryMessage : IMessageQueueMessage
    {
        private readonly string _gatewayName;
        private readonly string _deviceId;
        private readonly string _deviceName;

        public LowBatteryMessage(string gatewayName, IDevice device)
        {
            _gatewayName = gatewayName;
            _deviceId = device.Id;
            _deviceName = device.Name;
        }

        public string GatewayName => _gatewayName;
        public string DeviceId => _deviceId;
        public string DeviceName => _deviceName;
    }
}