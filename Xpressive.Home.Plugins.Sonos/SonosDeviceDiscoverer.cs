using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml;
using Xpressive.Home.Contracts.Services;

namespace Xpressive.Home.Plugins.Sonos
{
    internal sealed class SonosDeviceDiscoverer : ISonosDeviceDiscoverer
    {
        private readonly object _lock = new object();
        private readonly HashSet<string> _detectedSonosIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly ISonosSoapClient _soapClient;

        public SonosDeviceDiscoverer(IUpnpDeviceDiscoveringService upnpDeviceDiscoveringService, ISonosSoapClient soapClient)
        {
            _soapClient = soapClient;

            upnpDeviceDiscoveringService.DeviceFound += OnUpnpDeviceFound;
        }

        public event EventHandler<SonosDevice> DeviceFound;

        private async void OnUpnpDeviceFound(object sender, IUpnpDeviceResponse e)
        {
            if (e.Server.IndexOf("sonos", StringComparison.OrdinalIgnoreCase) < 0)
            {
                return;
            }

            await CreateDeviceAsync(e.Location);
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
            var name = document.SelectSingleNode("//upnp:modelName", namespaceManager)?.InnerText;

            lock (_lock)
            {
                if (string.IsNullOrEmpty(id) || _detectedSonosIds.Contains(id))
                {
                    return;
                }

                _detectedSonosIds.Add(id);
            }

            var zoneName = await GetZoneNameAsync(ip, port) ?? string.Empty;
            var isMaster = await GetIsMaster(ip, port);

            var device = new SonosDevice(id, ip, name)
            {
                Zone = zoneName,
                IsMaster = isMaster
            };

            OnDeviceFound(device);
        }

        private async Task<string> GetZoneNameAsync(string ip, int port)
        {
            const string action = "urn:upnp-org:serviceId:DeviceProperties#GetZoneAttributes";
            const string body = "<u:GetZoneAttributes xmlns:u=\"urn:schemas-upnp-org:service:DeviceProperties:1\"></u:GetZoneAttributes>";
            var uri = new Uri($"http://{ip}:{port}/DeviceProperties/Control");

            var document = await _soapClient.PostRequestAsync(uri, action, body);
            return document.SelectSingleNode("//CurrentZoneName")?.InnerText;
        }

        private async Task<bool> GetIsMaster(string ip, int port)
        {
            const string action = "uurn:schemas-upnp-org:service:AVTransport:1#GetPositionInfo";
            const string body = "<u:GetPositionInfo xmlns:u=\"urn:schemas-upnp-org:service:AVTransport:1\"><InstanceID>0</InstanceID><Channel>Master</Channel></u:GetPositionInfo>";
            var uri = new Uri($"http://{ip}:{port}/MediaRenderer/AVTransport/Control");

            var document = await _soapClient.PostRequestAsync(uri, action, body);
            var track = document.SelectSingleNode("//TrackURI")?.InnerText.ToLowerInvariant();

            return string.IsNullOrEmpty(track) || !track.StartsWith("x-rincon");
        }

        private void OnDeviceFound(SonosDevice e)
        {
            DeviceFound?.Invoke(this, e);
        }
    }
}