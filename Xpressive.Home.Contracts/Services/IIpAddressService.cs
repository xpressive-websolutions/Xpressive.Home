using System.Collections.Generic;

namespace Xpressive.Home.Contracts.Services
{
    public interface IIpAddressService
    {
        IEnumerable<string> GetIpAddresses();

        IEnumerable<string> GetOtherIpAddresses();
        IEnumerable<string> GetOtherIpAddresses(string ipAddress);
    }
}
