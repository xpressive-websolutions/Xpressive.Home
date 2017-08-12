using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Xml;
using Polly;
using Xpressive.Home.Contracts.Messaging;

namespace Xpressive.Home.Plugins.Sonos
{
    internal sealed class SonosDeviceDiscoverer : ISonosDeviceDiscoverer, IMessageQueueListener<NetworkDeviceFoundMessage>
    {
        private readonly object _lock = new object();
        private readonly HashSet<string> _detectedSonosIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public event EventHandler<SonosDevice> DeviceFound;

        public void Notify(NetworkDeviceFoundMessage message)
        {
            string server;
            string location;
            if (!message.Values.TryGetValue("Server", out server) ||
                !message.Values.TryGetValue("Location", out location) ||
                server.IndexOf("sonos", StringComparison.OrdinalIgnoreCase) < 0)
            {
                return;
            }

            var policy = Policy
                .Handle<WebException>()
                .WaitAndRetry(new[]
                {
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(2),
                    TimeSpan.FromSeconds(5),
                });

            policy.Execute(() => CreateDevice(location));
        }

        private void CreateDevice(string deviceDescriptionXmlPath)
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
            var model = document.SelectSingleNode("//upnp:modelNumber", namespaceManager)?.InnerText;

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

            var device = new SonosDevice(id, ip, name);
            device.Services.AddRange(upnpServices);
            device.Type = model;

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

        private void OnDeviceFound(SonosDevice e)
        {
            DeviceFound?.Invoke(this, e);
        }
    }
}
