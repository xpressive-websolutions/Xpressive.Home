using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RestSharp.Extensions.MonoHttp;
using Xpressive.Home.Contracts.Gateway;
using Action = Xpressive.Home.Contracts.Gateway.Action;

namespace Xpressive.Home.Plugins.Sonos
{
    internal class SonosGateway : GatewayBase, ISonosGateway
    {
        private readonly ISonosSoapClient _soapClient;

        public SonosGateway(ISonosDeviceDiscoverer deviceDiscoverer, ISonosSoapClient soapClient) : base("Sonos")
        {
            _soapClient = soapClient;
            _actions.Add(new Action("Play"));
            _actions.Add(new Action("Pause"));
            _actions.Add(new Action("Stop"));
            _actions.Add(new Action("Play Radio") { Fields = { "Stream", "Title" } });
            _actions.Add(new Action("Play File") { Fields = { "File", "Title", "Album" } });

            _canCreateDevices = false;
            deviceDiscoverer.DeviceFound += (s, e) => _devices.Add(e);
        }

        public override IDevice CreateEmptyDevice()
        {
            throw new NotSupportedException();
        }

        public IEnumerable<SonosDevice> GetDevices()
        {
            return Devices.OfType<SonosDevice>();
        }

        public async void Play(SonosDevice device)
        {
            await ExecuteInternal(device, new Action("Play"), null);
        }

        public async void Pause(SonosDevice device)
        {
            await ExecuteInternal(device, new Action("Pause"), null);
        }

        public async void Stop(SonosDevice device)
        {
            await ExecuteInternal(device, new Action("Stop"), null);
        }

        public async void PlayRadio(SonosDevice device, string stream, string title)
        {
            var parameters = new Dictionary<string, string>
            {
                {"Stream", stream},
                {"Title", title}
            };

            await ExecuteInternal(device, new Action("Play Radio"), parameters);
        }

        public async void PlayFile(SonosDevice device, string file, string title, string album)
        {
            var parameters = new Dictionary<string, string>
            {
                {"File", file},
                {"Title", title},
                {"Album", album}
            };

            await ExecuteInternal(device, new Action("Play File"), parameters);
        }

        protected override async Task ExecuteInternal(IDevice device, IAction action, IDictionary<string, string> values)
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
                        var url = ReplaceScheme(stream, "x-rincon-mp3radio");
                        await SendUrl(d, url, metadata);
                        await SendAvTransportControl(d, "Play");
                    }
                    break;
                case "play file":
                    if (!string.IsNullOrEmpty(file))
                    {
                        var metadata = GetFileMetadata(file, album);
                        var url = ReplaceScheme(file, "x-file-cifs");
                        await SendUrl(d, url, metadata);
                        await SendAvTransportControl(d, "Play");
                    }
                    break;
            }
        }

        private string ReplaceScheme(string url, string scheme)
        {
            var uri = new Uri(url);
            var path = HttpUtility.UrlDecode(uri.PathAndQuery);
            return $"{scheme}://{uri.Authority}{path}";
        }

        private async Task SendUrl(SonosDevice device, string url, string metadata)
        {
            metadata = HttpUtility.HtmlEncode(metadata);
            url = HttpUtility.HtmlEncode(url);

            var uri = $"http://{device.IpAddress}:1400/MediaRenderer/AVTransport/Control";
            var action = "urn:schemas-upnp-org:service:AVTransport:1#SetAVTransportURI";
            var body = $"<InstanceID>0</InstanceID><CurrentURI>{url}</CurrentURI><CurrentURIMetaData>{metadata}</CurrentURIMetaData>";
            var outerBody = $"<u:SetAVTransportURI xmlns:u=\"urn:schemas-upnp-org:service:AVTransport:1\">{body}</u:SetAVTransportURI>";
            await _soapClient.PostRequestAsync(new Uri(uri), action, outerBody);
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
            await _soapClient.PostRequestAsync(new Uri(uri), action, outerBody);
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
}
