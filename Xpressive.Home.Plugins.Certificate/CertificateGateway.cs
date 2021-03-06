﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Xpressive.Home.Contracts.Gateway;
using Xpressive.Home.Contracts.Messaging;

namespace Xpressive.Home.Plugins.Certificate
{
    internal sealed class CertificateGateway : GatewayBase, ICertificateGateway
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(CertificateGateway));
        private readonly IMessageQueue _messageQueue;

        public CertificateGateway(IMessageQueue messageQueue) : base("Certificate")
        {
            _messageQueue = messageQueue;

            _canCreateDevices = true;
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

        public override async Task StartAsync(CancellationToken cancellationToken)
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

                using (var response = (HttpWebResponse) await request.GetResponseAsync())
                {
                    var cert = new X509Certificate2(request.ServicePoint.Certificate);

                    device.FriendlyName = cert.FriendlyName;
                    device.HasPrivateKey = cert.HasPrivateKey;
                    device.Issuer = cert.Issuer;
                    device.NotAfter = cert.NotAfter.ToUniversalTime();
                    device.NotBefore = cert.NotBefore.ToUniversalTime();
                    device.SignatureAlgorithm = cert.SignatureAlgorithm.FriendlyName;
                    device.Subject = cert.Subject;
                    device.Thumbprint = cert.Thumbprint;

                    _messageQueue.Publish(new UpdateVariableMessage(Name, device.Id, "FriendlyName", cert.FriendlyName));
                    _messageQueue.Publish(new UpdateVariableMessage(Name, device.Id, "HasPrivateKey", cert.HasPrivateKey));
                    _messageQueue.Publish(new UpdateVariableMessage(Name, device.Id, "Issuer", cert.Issuer));
                    _messageQueue.Publish(new UpdateVariableMessage(Name, device.Id, "NotAfter", cert.NotAfter.ToUniversalTime().ToString("s")));
                    _messageQueue.Publish(new UpdateVariableMessage(Name, device.Id, "NotBefore", cert.NotBefore.ToUniversalTime().ToString("s")));
                    _messageQueue.Publish(new UpdateVariableMessage(Name, device.Id, "SignatureAlgorithm", cert.SignatureAlgorithm.FriendlyName));
                    _messageQueue.Publish(new UpdateVariableMessage(Name, device.Id, "Subject", cert.Subject));
                    _messageQueue.Publish(new UpdateVariableMessage(Name, device.Id, "Thumbprint", cert.Thumbprint));

                    response.Close();
                }
            }
            catch (Exception e)
            {
                _log.Error(e.Message, e);
            }
        }
    }
}
