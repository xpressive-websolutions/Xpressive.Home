using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jishi.Intel.SonosUPnP;

namespace Xpressive.Home.ProofOfConcept.Gateways.Sonos
{
    internal class SonosGateway : GatewayBase
    {
        private readonly object _deviceLock = new object();
        private readonly SonosDiscovery _discovery;

        public SonosGateway() : base("Sonos")
        {
            _actions.Add(new Action("Play"));


            _discovery = new SonosDiscovery();
            _discovery.TopologyChanged += () =>
            {
                Console.WriteLine("SONOS topoloy changed");

                foreach (var sonosPlayer in _discovery.Players)
                {
                    lock (_deviceLock)
                    {
                        if (_devices.Any(d => d.Name.Equals(sonosPlayer.Name, StringComparison.Ordinal)))
                        {
                            continue;
                        }

                        if (sonosPlayer.Device == null)
                        {
                            continue;
                        }

                        _devices.Add(new SonosDevice(sonosPlayer.UUID, sonosPlayer.BaseUrl.Host, sonosPlayer.Name));
                    }
                }
            };
            _discovery.StartScan();
        }

        protected override Task ExecuteInternal(DeviceBase device, IAction action, IDictionary<string, string> values)
        {
            throw new NotImplementedException();
        }

        protected override Task<string> GetInternal(DeviceBase device, PropertyBase property)
        {
            throw new NotImplementedException();
        }

        protected override Task SetInternal(DeviceBase device, PropertyBase property, string value)
        {
            throw new NotImplementedException();
        }
    }

    public class SonosDevice : DeviceBase
    {
        private readonly string _ipAddress;

        public SonosDevice(string id, string ipAddress, string name) : base(id, name)
        {
            _ipAddress = ipAddress;
        }

        public string IpAddress => _ipAddress;
    }
}
