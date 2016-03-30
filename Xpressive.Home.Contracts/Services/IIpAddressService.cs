using System.Collections.Generic;

namespace Xpressive.Home.Contracts.Services
{
    public interface IIpAddressService
    {
        string GetIpAddress();

        IEnumerable<string> GetOtherIpAddresses();
        IEnumerable<string> GetOtherIpAddresses(string ipAddress);
    }
}