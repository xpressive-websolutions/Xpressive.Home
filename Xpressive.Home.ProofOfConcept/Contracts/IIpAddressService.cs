using System.Collections.Generic;

namespace Xpressive.Home.ProofOfConcept
{
    public interface IIpAddressService
    {
        string GetIpAddress();

        IEnumerable<string> GetOtherIpAddresses();
        IEnumerable<string> GetOtherIpAddresses(string ipAddress);
    }
}