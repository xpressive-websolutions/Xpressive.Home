using System;
using System.Linq;

namespace Xpressive.Home.Contracts.Services
{
    public sealed class NetworkDevice
    {
        public static NetworkDevice Create(string ipAddress, string macAddress)
        {
            var mac = macAddress
                .Replace(":", string.Empty)
                .Replace("-", string.Empty)
                .Where((_, i) => i % 2 == 0)
                .Select((_, i) => macAddress.Substring(i * 3, 2))
                .Select(c => Convert.ToByte(c, 16))
                .ToArray();

            var device = new NetworkDevice(ipAddress, mac);
            return device;
        }

        public NetworkDevice(string ipAddress, byte[] macAddress)
        {
            IpAddress = ipAddress;
            MacAddress = macAddress;
        }

        public string IpAddress { get; }

        public byte[] MacAddress { get; }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) { return false; }
            if (ReferenceEquals(this, obj)) { return true; }
            return obj is NetworkDevice && Equals((NetworkDevice) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((IpAddress?.GetHashCode() ?? 0)*397) ^ (MacAddress?.GetHashCode() ?? 0);
            }
        }

        public override string ToString()
        {
            return $"NetworkDevice {IpAddress} {string.Join(":", MacAddress.Select(b => b.ToString("x2")))}";
        }

        private bool Equals(NetworkDevice other)
        {
            return string.Equals(IpAddress, other.IpAddress) && Equals(MacAddress, other.MacAddress);
        }
    }
}
