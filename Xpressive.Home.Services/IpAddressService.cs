using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Xpressive.Home.Contracts.Services;

namespace Xpressive.Home.Services
{
    internal class IpAddressService : IIpAddressService
    {
        public IEnumerable<string> GetOtherIpAddresses()
        {
            var ipAddress = GetIpAddress();
            return GetOtherIpAddresses(ipAddress);
        }

        public IEnumerable<string> GetOtherIpAddresses(string ipAddress)
        {
            var parts = ipAddress.Split('.');
            var prefix = string.Join(".", parts.Take(3));

            for (var i = 0; i < 256; i++)
            {
                var address = $"{prefix}.{i}";

                if (!string.Equals(address, ipAddress, StringComparison.Ordinal))
                {
                    yield return address;
                }
            }
        }

        public string GetIpAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());

            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }

            return string.Empty;
        }
    }
}