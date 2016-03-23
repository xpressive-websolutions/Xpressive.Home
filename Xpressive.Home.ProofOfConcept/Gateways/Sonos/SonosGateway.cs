using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RestSharp.Extensions.MonoHttp;
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
            _actions.Add(new Action("Play Radio") { Fields = { "Stream", "Title" } });
            _actions.Add(new Action("Play File") { Fields = { "File", "Title", "Album" } });

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
            string title;
            string album;

            values.TryGetValue("Stream", out stream);
            values.TryGetValue("File", out file);
            values.TryGetValue("Title", out title);
            values.TryGetValue("Album", out album);

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
                    if (!string.IsNullOrEmpty(stream) && !string.IsNullOrEmpty(title))
                    {
                        var metadata = GetRadioMetadata(title);
                        await SendUrl(d, stream, metadata);
                    }
                    break;
                case "play file":
                    if (!string.IsNullOrEmpty(file))
                    {
                        var metadata = GetFileMetadata("file", "filealbum");
                        await SendUrl(d, file, metadata);
                    }
                    break;
            }
        }

        private async Task SendUrl(SonosDevice device, string url, string metadata)
        {
            metadata = HttpUtility.HtmlEncode(metadata);
            url = HttpUtility.HtmlEncode(url);

            var uri = $"http://{device.IpAddress}:1400/MediaRenderer/AVTransport/Control";
            var action = "urn:schemas-upnp-org:service:AVTransport:1#SetAVTransportURI";
            var body = $"<InstanceID>0</InstanceID><CurrentURI>{url}</CurrentURI><CurrentURIMetaData>{metadata}</CurrentURIMetaData>";
            var outerBody = $"<u:SetAVTransportURI xmlns:u=\"urn:schemas-upnp-org:service:AVTransport:1\">{body}</u:SetAVTransportURI>";
            var soapClient = new SonosSoapClient();
            await soapClient.PostRequest(new Uri(uri), action, outerBody);
        }

        private async Task SendAvTransportControl(SonosDevice device, string command)
        {
            var body = "<InstanceID>0</InstanceID><Speed>1</Speed>";
            await SendAvTransportControl(device, command, body);
        }

        private async Task SendAvTransportControl(SonosDevice device, string command, string body)
        {
            var uri = $"http://{device.IpAddress}:1400/MediaRenderer/AVTransport/Control";
            var action = $"urn:schemas-upnp-org:service:AVTransport:1#{command}";
            var outerBody = $"<u:{command} xmlns:u=\"urn:schemas-upnp-org:service:AVTransport:1\">{body}</u:{command}>";
            var soapClient = new SonosSoapClient();
            await soapClient.PostRequest(new Uri(uri), action, outerBody);
        }

        private string GetRadioMetadata(string title)
        {
            var didl = $"<DIDL-Lite xmlns:dc=\"http://purl.org/dc/elements/1.1/\" xmlns:upnp=\"urn:schemas-upnp-org:metadata-1-0/upnp/\" xmlns:r=\"urn:schemas-rinconnetworks-com:metadata-1-0/\" xmlns=\"urn:schemas-upnp-org:metadata-1-0/DIDL-Lite/\"><item id=\"R:0/0/0\" parentID=\"R:0/0\" restricted=\"true\"><dc:title>{title}</dc:title><upnp:class>object.item.audioItem.audioBroadcast</upnp:class><desc id=\"cdudn\" nameSpace=\"urn:schemas-rinconnetworks-com:metadata-1-0/\">SA_RINCON65031_</desc></item></DIDL-Lite>";
            return didl;
        }

        private string GetFileMetadata(string title, string album)
        {
            return null;
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
