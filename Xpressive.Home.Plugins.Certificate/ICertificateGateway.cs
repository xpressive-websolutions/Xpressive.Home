using System.Collections.Generic;

namespace Xpressive.Home.Plugins.Certificate
{
    internal interface ICertificateGateway
    {
        IEnumerable<CertificateDevice> GetDevices();
    }
}
