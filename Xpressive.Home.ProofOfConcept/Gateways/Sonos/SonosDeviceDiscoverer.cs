using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace Xpressive.Home.ProofOfConcept.Gateways.Sonos
{
    internal sealed class SonosDeviceDiscoverer
    {
        private readonly object _lock = new object();
        private readonly HashSet<string> _detectedSonosIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly SonosSoapClient _soapClient = new SonosSoapClient();

        public event EventHandler<SonosDevice> DeviceFound;

        public async Task StartDiscoverAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await FindUpnpDevicesAsync(r => HandleSsdpResponse(r, async d => await CreateDeviceAsync(d)));
                await Task.Delay(TimeSpan.FromMinutes(10), cancellationToken);
            }
        }

        private async Task CreateDeviceAsync(string deviceDescriptionXmlPath)
        {
            var document = new XmlDocument();
            document.Load(deviceDescriptionXmlPath);

            var url = new Uri(deviceDescriptionXmlPath, UriKind.Absolute);
            var ip = url.Host;
            var port = url.Port;
            var namespaceManager = new XmlNamespaceManager(document.NameTable);
            namespaceManager.AddNamespace("upnp", "urn:schemas-upnp-org:device-1-0");
            var id = document.SelectSingleNode("//upnp:UDN", namespaceManager)?.InnerText;

            lock (_lock)
            {
                if (string.IsNullOrEmpty(id) || _detectedSonosIds.Contains(id))
                {
                    return;
                }

                _detectedSonosIds.Add(id);
            }

            var name = await GetZoneNameAsync(ip, port) ?? string.Empty;
            var isMaster = await GetIsMaster(ip, port);

            // TODO: name should not be the zoneName

            var device = new SonosDevice(id, ip, name)
            {
                Zone = name,
                IsMaster = isMaster
            };

            OnDeviceFound(device);
        }

        private async Task<string> GetZoneNameAsync(string ip, int port)
        {
            const string action = "urn:upnp-org:serviceId:DeviceProperties#GetZoneAttribute";
            const string body = "<u:GetZoneAttributes xmlns:u=\"urn:schemas-upnp-org:service:DeviceProperties:1\"></u:GetZoneAttributes>";
            var uri = new Uri($"http://{ip}:{port}/DeviceProperties/Control");

            var document = await _soapClient.PostRequest(uri, action, body);
            return document.SelectSingleNode("//CurrentZoneName")?.InnerText;
        }

        private async Task<bool> GetIsMaster(string ip, int port)
        {
            const string action = "uurn:schemas-upnp-org:service:AVTransport:1#GetPositionInfo";
            const string body = "<u:GetPositionInfo xmlns:u=\"urn:schemas-upnp-org:service:AVTransport:1\"><InstanceID>0</InstanceID><Channel>Master</Channel></u:GetPositionInfo>";
            var uri = new Uri($"http://{ip}:{port}/MediaRenderer/AVTransport/Control");

            var document = await _soapClient.PostRequest(uri, action, body);
            var track = document.SelectSingleNode("//TrackURI")?.InnerText?.ToLowerInvariant();

            return !string.IsNullOrEmpty(track) && !track.StartsWith("x-rincon");
        }

        private void HandleSsdpResponse(string ssdpResponse, Action<string> handleSonosDeviceDescription)
        {
            Console.WriteLine(ssdpResponse);
            Console.WriteLine();

            if (!ssdpResponse.Contains("sonos"))
            {
                return;
            }

            var locationStart = ssdpResponse.IndexOf("location:", StringComparison.Ordinal) + 10;
            var locationEnd = ssdpResponse.IndexOf("\r", locationStart, StringComparison.Ordinal);
            var location = ssdpResponse.Substring(locationStart, locationEnd - locationStart);

            handleSonosDeviceDescription(location);
        }

        private async Task FindUpnpDevicesAsync(Action<string> handleSsdpResponse)
        {
            const string multicastIpAddress = "239.255.255.250";
            const int multicastPort = 1900;

            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);
                var payload =
                    "M-SEARCH * HTTP/1.1\r\n" +
                    $"HOST: {multicastIpAddress}:{multicastPort}\r\n" +
                    "ST:upnp:rootdevice\r\n" +
                    "MAN:\"ssdp:discover\"\r\n" +
                    "MX:5\r\n\r\n";
                var data = Encoding.ASCII.GetBytes(payload);
                var timeout = DateTime.UtcNow + TimeSpan.FromSeconds(5);
                var buffer = new byte[32768];

                socket.SendTo(data, new IPEndPoint(IPAddress.Parse(multicastIpAddress), multicastPort));

                while (DateTime.UtcNow < timeout)
                {
                    await Task.Delay(10);

                    if (socket.Available > 0)
                    {
                        var length = socket.Receive(buffer);
                        var response = Encoding.ASCII.GetString(buffer, 0, length).ToLowerInvariant();
                        handleSsdpResponse(response);
                    }
                }

                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
            }
        }

        private void OnDeviceFound(SonosDevice e)
        {
            DeviceFound?.Invoke(this, e);
        }
    }
}
