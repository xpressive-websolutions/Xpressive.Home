using System;
using System.Collections.Generic;
using System.Linq;
using log4net;
using Xpressive.Home.Contracts.Automation;

namespace Xpressive.Home.Plugins.Certificate
{
    internal sealed class CertificateScriptObjectProvider : IScriptObjectProvider
    {
        private readonly ICertificateGateway _gateway;

        public CertificateScriptObjectProvider(ICertificateGateway gateway)
        {
            _gateway = gateway;
        }

        public IEnumerable<Tuple<string, object>> GetObjects()
        {
            yield break;
        }

        public IEnumerable<Tuple<string, Delegate>> GetDelegates()
        {
            // certificate("id").getFriendlyName()

            var deviceResolver = new Func<string, CertificateScriptObject>(id =>
            {
                var device = _gateway.GetDevices().SingleOrDefault(d => d.Id.Equals(id));
                return new CertificateScriptObject(device);
            });

            yield return new Tuple<string, Delegate>("certificate", deviceResolver);
        }

        public class CertificateScriptObject
        {
            private static readonly ILog _log = LogManager.GetLogger(typeof(CertificateScriptObject));
            private readonly CertificateDevice _device;

            public CertificateScriptObject(CertificateDevice device)
            {
                _device = device;
            }

            public object getFriendlyName()
            {
                if (_device == null)
                {
                    _log.Warn("Unable to get variable value because the device was not found.");
                    return null;
                }

                return _device.FriendlyName;
            }

            public object getHasPrivateKey()
            {
                if (_device == null)
                {
                    _log.Warn("Unable to get variable value because the device was not found.");
                    return null;
                }

                return _device.HasPrivateKey;
            }

            public object getIssuer()
            {
                if (_device == null)
                {
                    _log.Warn("Unable to get variable value because the device was not found.");
                    return null;
                }

                return _device.Issuer;
            }

            public object getNotAfter()
            {
                if (_device == null)
                {
                    _log.Warn("Unable to get variable value because the device was not found.");
                    return null;
                }

                return _device.NotAfter;
            }

            public object getNotBefore()
            {
                if (_device == null)
                {
                    _log.Warn("Unable to get variable value because the device was not found.");
                    return null;
                }

                return _device.NotBefore;
            }

            public object getSignatureAlgorithm()
            {
                if (_device == null)
                {
                    _log.Warn("Unable to get variable value because the device was not found.");
                    return null;
                }

                return _device.SignatureAlgorithm;
            }

            public object getThumbprint()
            {
                if (_device == null)
                {
                    _log.Warn("Unable to get variable value because the device was not found.");
                    return null;
                }

                return _device.Thumbprint;
            }

            public object getSubject()
            {
                if (_device == null)
                {
                    _log.Warn("Unable to get variable value because the device was not found.");
                    return null;
                }

                return _device.Subject;
            }
        }
    }
}
