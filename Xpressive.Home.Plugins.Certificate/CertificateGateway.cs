using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using Xpressive.Home.Contracts.Gateway;
using Xpressive.Home.Contracts.Messaging;

namespace Xpressive.Home.Plugins.Certificate
{
    internal sealed class CertificateGateway : GatewayBase, ICertificateGateway
    {
        public CertificateGateway(IMessageQueue messageQueue, IDevicePersistingService persistingService)
            : base(messageQueue, "Certificate", true, persistingService)
        {
        }

        public override IDevice CreateEmptyDevice()
        {
            return new CertificateDevice();
        }

        public IEnumerable<CertificateDevice> GetDevices()
        {
            return Devices.OfType<CertificateDevice>();
        }

        public override IEnumerable<IAction> GetActions(IDevice device)
        {
            yield break;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ContinueWith(_ => { });

            await LoadDevicesAsync((id, name) => new CertificateDevice { Id = id, Name = name });

            while (!cancellationToken.IsCancellationRequested)
            {
                foreach (var device in GetDevices())
                {
                    await UpdateVariables(device);
                }

                await Task.Delay(TimeSpan.FromHours(1), cancellationToken).ContinueWith(_ => { });
            }
        }

        protected override Task ExecuteInternalAsync(IDevice device, IAction action, IDictionary<string, string> values)
        {
            throw new NotSupportedException();
        }

        private async Task UpdateVariables(CertificateDevice device)
        {
            try
            {
                var request = (HttpWebRequest)WebRequest.Create(device.HostName);
                request.AllowAutoRedirect = true;
                request.ServerCertificateValidationCallback += (sender, certificate, chain, errors) =>
                {
                    var cert = new X509Certificate2(certificate);
                    device.FriendlyName = cert.FriendlyName;
                    device.HasPrivateKey = cert.HasPrivateKey;
                    device.Issuer = cert.Issuer;
                    device.NotAfter = cert.NotAfter.ToUniversalTime();
                    device.NotBefore = cert.NotBefore.ToUniversalTime();
                    device.SignatureAlgorithm = cert.SignatureAlgorithm.FriendlyName;
                    device.Subject = cert.Subject;
                    device.Thumbprint = cert.Thumbprint;

                    MessageQueue.Publish(new UpdateVariableMessage(Name, device.Id, "FriendlyName", cert.FriendlyName));
                    MessageQueue.Publish(new UpdateVariableMessage(Name, device.Id, "HasPrivateKey", cert.HasPrivateKey));
                    MessageQueue.Publish(new UpdateVariableMessage(Name, device.Id, "Issuer", cert.Issuer));
                    MessageQueue.Publish(new UpdateVariableMessage(Name, device.Id, "NotAfter", cert.NotAfter.ToUniversalTime().ToString("s")));
                    MessageQueue.Publish(new UpdateVariableMessage(Name, device.Id, "NotBefore", cert.NotBefore.ToUniversalTime().ToString("s")));
                    MessageQueue.Publish(new UpdateVariableMessage(Name, device.Id, "SignatureAlgorithm", cert.SignatureAlgorithm.FriendlyName));
                    MessageQueue.Publish(new UpdateVariableMessage(Name, device.Id, "Subject", cert.Subject));
                    MessageQueue.Publish(new UpdateVariableMessage(Name, device.Id, "Thumbprint", cert.Thumbprint));

                    return true;
                };

                using (var response = (HttpWebResponse)await request.GetResponseAsync())
                {
                    response.Close();
                }
            }
            catch (Exception e)
            {
                Log.Error(e, e.Message);
            }
        }
    }
}
