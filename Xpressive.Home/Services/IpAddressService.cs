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
            return GetIpAddresses()
                .SelectMany(GetOtherIpAddresses)
                .Distinct()
                .ToList();
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

        public IEnumerable<string> GetIpAddresses()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            var ips = host.AddressList
                .Where(ip => ip.AddressFamily == AddressFamily.InterNetwork)
                .Select(ip => ip.ToString())
                .ToList();

            return ips;
        }
    }
}
