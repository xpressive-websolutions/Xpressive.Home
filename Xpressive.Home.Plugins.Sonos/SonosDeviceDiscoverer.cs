using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

            var upnpServices = GetServices(document, namespaceManager).ToList();

            foreach (var service in upnpServices)
            {
                var serviceUrl = $"http://{ip}:{port}{service.DescriptionUrl}";
                var actions = GetActions(serviceUrl);
                service.Actions.AddRange(actions);
            }

            var zoneName = await GetZoneNameAsync(ip, port, upnpServices) ?? string.Empty;
            var isMaster = await GetIsMaster(ip, port, upnpServices);

            var device = new SonosDevice(id, ip, name)
            {
                Zone = zoneName,
                IsMaster = isMaster
            };
            device.Services.AddRange(upnpServices);

            OnDeviceFound(device);
        }

        private IEnumerable<UpnpAction> GetActions(string url)
        {
            var document = new XmlDocument();
            document.Load(url);
            var namespaceManager = new XmlNamespaceManager(document.NameTable);
            namespaceManager.AddNamespace("upnp", "urn:schemas-upnp-org:service-1-0");

            var actions = document.SelectNodes("//upnp:actionList/upnp:action", namespaceManager)?.OfType<XmlNode>().ToList();

            if (actions == null)
            {
                yield break;
            }

            foreach (var action in actions)
            {
                var childNodes = action.ChildNodes.OfType<XmlNode>().ToList();
                var name = childNodes.SingleOrDefault(n => n.Name.Equals("name"))?.InnerText;
                var arguments = childNodes.SingleOrDefault(n => n.Name.Equals("argumentList"))?.ChildNodes;
                var dto = new UpnpAction {Name = name};

                if (arguments == null)
                {
                    yield return dto;
                    continue;
                }

                foreach (var argument in arguments.Cast<XmlNode>().Where(n => n.HasChildNodes))
                {
                    var argumentChildNodes = argument.ChildNodes.Cast<XmlNode>().ToList();
                    var argumentName = argumentChildNodes.SingleOrDefault(n => n.Name.Equals("name"))?.InnerText;
                    var direction = argumentChildNodes.SingleOrDefault(n => n.Name.Equals("direction"))?.InnerText;

                    if ("in".Equals(direction))
                    {
                        dto.InputArguments.Add(argumentName);
                    }
                    else if ("out".Equals(direction))
                    {
                        dto.OutputArguments.Add(argumentName);
                    }
                }

                yield return dto;
            }
        }

        private IEnumerable<UpnpService> GetServices(XmlDocument document, XmlNamespaceManager namespaceManager)
        {
            var services = document.SelectNodes("//upnp:device/upnp:serviceList/upnp:service", namespaceManager)?.OfType<XmlNode>().ToList();

            if (services == null)
            {
                yield break;
            }

            foreach (var service in services)
            {
                var childNodes = service.ChildNodes.OfType<XmlNode>().ToList();

                yield return new UpnpService
                {
                    Type = childNodes.SingleOrDefault(n => n.Name.Equals("serviceType"))?.InnerText,
                    Id = childNodes.SingleOrDefault(n => n.Name.Equals("serviceId"))?.InnerText,
                    ControlUrl = childNodes.SingleOrDefault(n => n.Name.Equals("controlURL"))?.InnerText,
                    DescriptionUrl = childNodes.SingleOrDefault(n => n.Name.Equals("SCPDURL"))?.InnerText
                };
            }
        }

        private async Task<string> GetZoneNameAsync(string ip, int port, List<UpnpService> services)
        {
            var service = services.Single(s => s.Id.Contains("DeviceProperties"));
            var action = service.Actions.Single(s => s.Name.Equals("GetZoneAttributes"));
            var values = new Dictionary<string, string>();

            var result = await Execute(ip, port, service, action, values);
            string currentZoneName;

            if (result.TryGetValue("CurrentZoneName", out currentZoneName))
            {
                return currentZoneName;
            }

            return string.Empty;
        }

        private async Task<bool> GetIsMaster(string ip, int port, List<UpnpService> services)
        {
            var service = services.Single(s => s.Id.Contains("AVTransport"));
            var action = service.Actions.Single(s => s.Name.Equals("GetPositionInfo"));
            var values = new Dictionary<string, string>
            {
                {"InstanceID", "0"}
            };

            var result = await Execute(ip, port, service, action, values);
            string trackUri;

            if (result.TryGetValue("TrackURI", out trackUri))
            {
                return string.IsNullOrEmpty(trackUri) || !trackUri.StartsWith("x-rincon:RINCON");
            }

            return false;
        }

        private async Task<Dictionary<string, string>> Execute(string ip, int port, UpnpService service, UpnpAction action, Dictionary<string, string> values)
        {
            var uri = new Uri($"http://{ip}:{port}{service.ControlUrl}");
            var soapAction = $"{service.Id}#{action.Name}";

            var body = new StringBuilder();
            body.Append($"<u:{action.Name} xmlns:u=\"{service.Type}\">");

            foreach (var argument in action.InputArguments)
            {
                string value;
                if (values.TryGetValue(argument, out value))
                {
                    body.Append($"<{argument}>{value}</{argument}>");
                }
            }
            body.Append($"</u:{action.Name}>");

            var document = await _soapClient.PostRequestAsync(uri, soapAction, body.ToString());
            var result = new Dictionary<string, string>();

            foreach (var argument in action.OutputArguments)
            {
                var value = document.SelectSingleNode("//" + argument)?.InnerText;
                result.Add(argument, value);
            }

            return result;
        }

        private void OnDeviceFound(SonosDevice e)
        {
            DeviceFound?.Invoke(this, e);
        }
    }
}
