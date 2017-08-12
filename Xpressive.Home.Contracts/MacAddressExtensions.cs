using System;
using System.Linq;

namespace Xpressive.Home.Contracts
{
    public static class MacAddressExtensions
    {
        private static readonly string[] _macAddressDelimiters = {":", "-"};

        public static string MacAddressToString(this byte[] bytes, string delimiter = "")
        {
            if (bytes == null || bytes.Length <= 0)
            {
                return string.Empty;
            }

            return string.Join(delimiter ?? string.Empty, bytes.Select(b => b.ToString("x2")));
        }

        public static byte[] MacAddressToBytes(this string macAddress)
        {
            if (string.IsNullOrEmpty(macAddress))
            {
                return new byte[0];
            }

            var mac = macAddress
                .RemoveMacAddressDelimiters()
                .Where((_, i) => i % 2 == 0)
                .Select((_, i) => macAddress.Substring(i * 3, 2))
                .Select(c => Convert.ToByte(c, 16))
                .ToArray();

            return mac;
        }

        public static string RemoveMacAddressDelimiters(this string macAddress)
        {
            foreach (var delimiter in _macAddressDelimiters)
            {
                macAddress = macAddress.Replace(delimiter, string.Empty);
            }

            return macAddress;
        }
    }
}
