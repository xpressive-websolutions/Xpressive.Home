using System;
using Xpressive.Home.Contracts.Gateway;

namespace Xpressive.Home.Plugins.Certificate
{
    internal sealed class CertificateDevice : DeviceBase
    {
        public CertificateDevice()
        {
            Icon = "fa fa-certificate";
        }

        [DeviceProperty(3)]
        public string HostName { get; set; }

        public string FriendlyName { get; set; }
        public bool HasPrivateKey { get; set; }
        public string Issuer { get; set; }
        public DateTime NotAfter { get; set; }
        public DateTime NotBefore { get; set; }
        public string SignatureAlgorithm { get; set; }
        public string Subject { get; set; }
        public string Thumbprint { get; set; }

        public override bool IsConfigurationValid()
        {
            if (string.IsNullOrEmpty(HostName))
            {
                return false;
            }

            if (!HostName.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return base.IsConfigurationValid();
        }
    }
}
