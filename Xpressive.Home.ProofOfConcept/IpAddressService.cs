using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace Xpressive.Home.ProofOfConcept
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
                yield return $"{prefix}.{i}";
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