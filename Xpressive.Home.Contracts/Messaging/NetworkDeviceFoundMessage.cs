using System;
using System.Collections.Generic;

namespace Xpressive.Home.Contracts.Messaging
{
    public sealed class NetworkDeviceFoundMessage : IMessageQueueMessage
    {
        private readonly string _protocol;
        private readonly string _ipAddress;
        private readonly byte[] _macAddress;
        private readonly string _friendlyName;
        private readonly string _manufacturer;
        private readonly Dictionary<string, string> _values;

        public NetworkDeviceFoundMessage(string protocol, string ipAddress, byte[] macAddress, string friendlyName = null, string manufacturer = null)
        {
            _protocol = protocol;
            _ipAddress = ipAddress;
            _macAddress = macAddress ?? new byte[0];
            _friendlyName = friendlyName ?? string.Empty;
            _manufacturer = manufacturer ?? string.Empty;
            _values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        public string Protocol => _protocol;
        public string IpAddress => _ipAddress;
        public byte[] MacAddress => _macAddress;
        public string FriendlyName => _friendlyName;
        public string Manufacturer => _manufacturer;
        public IDictionary<string, string> Values => _values;
    }
}
