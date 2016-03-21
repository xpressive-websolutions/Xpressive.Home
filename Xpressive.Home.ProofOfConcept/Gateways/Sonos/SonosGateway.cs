using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Xpressive.Home.ProofOfConcept.Contracts;

namespace Xpressive.Home.ProofOfConcept.Gateways.Sonos
{
    internal class SonosGateway : GatewayBase
    {
        private readonly object _deviceLock = new object();

        public SonosGateway() : base("Sonos")
        {
            _actions.Add(new Action("Play"));
            _actions.Add(new Action("Pause"));
            _actions.Add(new Action("Stop"));
            _actions.Add(new Action("Play Radio") { Fields = { "Stream" } });
            _actions.Add(new Action("Play File") { Fields = { "File" } });

            var sonosDeviceDiscoverer = new SonosDeviceDiscoverer();
            sonosDeviceDiscoverer.DeviceFound += (s, e) =>
            {
                lock (_deviceLock)
                {
                    if (_devices.Any(d => d.Id.Equals(e.Id, StringComparison.Ordinal)))
                    {
                        return;
                    }

                    _devices.Add(e);
                }
            };
            
            sonosDeviceDiscoverer.Discover();
        }

        public override bool IsConfigurationValid()
        {
            throw new NotImplementedException();
        }

        protected override async Task ExecuteInternal(DeviceBase device, IAction action, IDictionary<string, string> values)
        {
            var d = device as SonosDevice;

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
            }
        }

        private async Task SendAvTransportControl(SonosDevice device, string command)
        {
            var uri = $"http://{device.IpAddress}:1400/MediaRenderer/AVTransport/Control";
            var action = $"\"urn:schemas-upnp-org:service:AVTransport:1#{command}\"";
            var body = $"<u:{command} xmlns:u=\"urn:schemas-upnp-org:service:AVTransport:1\"><InstanceID>0</InstanceID><Speed>1</Speed></u:{command}>";
            var soapClient = new SonosSoapClient();
            await soapClient.PostRequest(new Uri(uri), action, body);
        }
    }

    internal class SonosSoapClient
    {
        public async Task<XmlDocument> PostRequest(Uri uri, string action, string body)
        {
            var request = WebRequest.CreateHttp(uri);
            request.Method = "POST";
            request.Headers.Add("SOAPACTION", action);
            request.ContentType = "text/xml; charset=\"utf-8\"";

            var message =
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                "<s:Envelope xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\" s:encodingStyle=\"http://schemas.xmlsoap.org/soap/encoding/\">" +
                $"<s:Body>{body}</s:Body>" +
                "</s:Envelope>\n";

            var payload = Encoding.UTF8.GetBytes(message);
            request.ContentLength = payload.Length;

            using (var stream = await request.GetRequestStreamAsync())
            {
                var data = Encoding.UTF8.GetBytes(message);
                await stream.WriteAsync(data, 0, data.Length);
            }

            using (var response = await request.GetResponseAsync())
            {
                using (var stream = response.GetResponseStream())
                {
                    using (var reader = new StreamReader(stream))
                    {
                        await stream.FlushAsync();
                        var xml = await reader.ReadToEndAsync();
                        var document = new XmlDocument();
                        document.LoadXml(SanitizeXmlString(xml));
                        return document;
                    }
                }
            }
        }

        private static string SanitizeXmlString(string xml)
        {
            var buffer = new StringBuilder(xml.Length);

            foreach (var c in xml.Where(XmlConvert.IsXmlChar))
            {
                buffer.Append(c);
            }

            return buffer.ToString();
        }
    }

    internal class SonosDeviceDiscoverer
    {
        public event EventHandler<SonosDevice> DeviceFound;

        public async Task Discover()
        {
            var ssdpResponses = await FindUpnpDevices();
            var sonosXmlFiles = GetSonosXmlFiles(ssdpResponses);
            var devices = await CreateDevices(sonosXmlFiles);

            foreach (var device in devices)
            {
                OnDeviceFound(device);
            }
        }

        private async Task<IEnumerable<SonosDevice>> CreateDevices(IEnumerable<string> xmlFiles)
        {
            var soapClient = new SonosSoapClient();
            var devices = new List<SonosDevice>();

            foreach (var xmlFile in xmlFiles)
            {
                var document = new XmlDocument();
                document.Load(xmlFile);

                var namespaceManager = new XmlNamespaceManager(document.NameTable);
                namespaceManager.AddNamespace("upnp", "urn:schemas-upnp-org:device-1-0");

                var id = document.SelectSingleNode("//upnp:UDN", namespaceManager).InnerText;
                var ip = new Uri(xmlFile, UriKind.Absolute).Host;

                var uri = new Uri($"http://{ip}:1400/DeviceProperties/Control");
                var action = "\"urn:upnp-org:serviceId:DeviceProperties#GetZoneAttributes\"";
                var body = "<u:GetZoneAttributes xmlns:u=\"urn:schemas-upnp-org:service:DeviceProperties:1\"></u:GetZoneAttributes>";

                var deviceProperties = await soapClient.PostRequest(uri, action, body);
                var name = deviceProperties.SelectSingleNode("//CurrentZoneName").InnerText;

                var device = new SonosDevice(id, ip, name);
                devices.Add(device);
            }

            return devices;
        }

        private IEnumerable<string> GetSonosXmlFiles(IEnumerable<string> upnpDevices)
        {
            foreach (var device in upnpDevices)
            {
                if (device.Contains("sonos"))
                {
                    var locationStart = device.IndexOf("location:", StringComparison.Ordinal) + 10;
                    var locationEnd = device.IndexOf("\r", locationStart, StringComparison.Ordinal);
                    var location = device.Substring(locationStart, locationEnd - locationStart);

                    yield return location;
                }
            }
        }

        private async Task<IEnumerable<string>> FindUpnpDevices()
        {
            const string multicastIpAddress = "239.255.255.250";
            const int multicastPort = 1900;

            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);
            var payload =
                "M-SEARCH * HTTP/1.1\r\n" +
                $"HOST: {multicastIpAddress}:{multicastPort}\r\n" +
                "ST:upnp:rootdevice\r\n" +
                "MAN:\"ssdp:discover\"\r\n" +
                "MX:2\r\n\r\n" +
                "HeaderEnd: CRLF";
            var data = Encoding.ASCII.GetBytes(payload);
            var responses = new List<string>();
            var timeout = DateTime.UtcNow + TimeSpan.FromSeconds(5);
            var buffer = new byte[32768];

            socket.SendTo(data, new IPEndPoint(IPAddress.Parse(multicastIpAddress), 1900));

            while (DateTime.UtcNow < timeout)
            {
                await Task.Delay(10);

                if (socket.Available > 0)
                {
                    var length = socket.Receive(buffer);
                    var response = Encoding.ASCII.GetString(buffer, 0, length).ToLowerInvariant();
                    responses.Add(response);
                }
            }

            socket.Shutdown(SocketShutdown.Both);
            socket.Close();

            return responses;
        }

        private void OnDeviceFound(SonosDevice e)
        {
            DeviceFound?.Invoke(this, e);
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
        public bool IsMaster { get; set; }
    }
}
