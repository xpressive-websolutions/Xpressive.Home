using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xpressive.Home.ProofOfConcept.Contracts;

namespace Xpressive.Home.ProofOfConcept.Gateways.Sonos
{
    internal class SonosGateway : GatewayBase
    {
        public SonosGateway() : base("Sonos")
        {
            _actions.Add(new Action("Play"));
            _actions.Add(new Action("Pause"));
            _actions.Add(new Action("Stop"));
            _actions.Add(new Action("Play Radio") { Fields = { "Stream" } });
            _actions.Add(new Action("Play File") { Fields = { "File" } });

            var sonosDeviceDiscoverer = new SonosDeviceDiscoverer();
            var cancellationToken = new CancellationTokenSource();
            sonosDeviceDiscoverer.DeviceFound += (s, e) => _devices.Add(e);
            sonosDeviceDiscoverer.StartDiscoverAsync(cancellationToken.Token);
        }

        public override bool IsConfigurationValid()
        {
            return true;
        }

        protected override async Task ExecuteInternal(DeviceBase device, IAction action, IDictionary<string, string> values)
        {
            var d = device as SonosDevice;
            string stream;
            string file;

            values.TryGetValue("Stream", out stream);
            values.TryGetValue("File", out file);

            if (d == null)
            {
                return;
            }

            switch (action.Name.ToLowerInvariant())
            {
                case "play":
                    await SendAvTransportControl(d, "Play");
                    break;
                case "pause":
                    await SendAvTransportControl(d, "Pause");
                    break;
                case "stop":
                    await SendAvTransportControl(d, "Stop");
                    break;
                case "play radio":
                    if (!string.IsNullOrEmpty(stream))
                    {
                        // TODO
                    }
                    break;
                case "play file":
                    if (!string.IsNullOrEmpty(file))
                    {
                        // TODO
                    }
                    break;
            }
        }

        private async Task SendAvTransportControl(SonosDevice device, string command)
        {
            var uri = $"http://{device.IpAddress}:1400/MediaRenderer/AVTransport/Control";
            var action = $"urn:schemas-upnp-org:service:AVTransport:1#{command}";
            var body = $"<u:{command} xmlns:u=\"urn:schemas-upnp-org:service:AVTransport:1\"><InstanceID>0</InstanceID><Speed>1</Speed></u:{command}>";
            var soapClient = new SonosSoapClient();
            await soapClient.PostRequest(new Uri(uri), action, body);
        }
    }

    public class SonosDevice : DeviceBase
    {
        private readonly string _ipAddress;

        public SonosDevice(string id, string ipAddress, string name)
        {
            _ipAddress = ipAddress;
            Id = id;
            Name = name;
        }

        public string IpAddress => _ipAddress;
        public string Type { get; set; }
        public string Zone { get; set; }
        public bool IsMaster { get; set; }
    }
}
