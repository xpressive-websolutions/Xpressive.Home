using System;
using System.Collections.Generic;
using System.Linq;
using Xpressive.Home.ProofOfConcept.Contracts;

namespace Xpressive.Home.ProofOfConcept
{
    internal class GatewayResolver : IGatewayResolver
    {
        private readonly object _lock = new object();
        private readonly Dictionary<string, IGateway> _gateways;

        public GatewayResolver()
        {
            _gateways = new Dictionary<string, IGateway>(StringComparer.Ordinal);
        }

        public void Register(IGateway gateway)
        {
            lock (_lock)
            {
                _gateways[gateway.Name] = gateway;
            }
        }

        public IGateway Resolve(string gatewayName)
        {
            lock (_lock)
            {
                IGateway gateway;
                if (_gateways.TryGetValue(gatewayName, out gateway))
                {
                    return gateway;
                }
            }

            return null;
        }

        public IEnumerable<IGateway> GetAll()
        {
            lock (_lock)
            {
                return _gateways.Values.ToList();
            }
        }
    }
}