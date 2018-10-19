using System.Collections.Generic;
using Xpressive.Home.Contracts.Gateway;

namespace Xpressive.Home.Plugins.Certificate
{
    internal interface ICertificateGateway : IGateway
    {
        IEnumerable<CertificateDevice> GetDevices();
    }
}
