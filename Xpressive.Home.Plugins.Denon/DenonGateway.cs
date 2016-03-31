using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using log4net;
using RestSharp;
using Xpressive.Home.Contracts.Gateway;
using Xpressive.Home.Contracts.Messaging;
using Xpressive.Home.Contracts.Services;
using Action = Xpressive.Home.Contracts.Gateway.Action;

namespace Xpressive.Home.Plugins.Denon
{
    internal class DenonGateway : GatewayBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof (DenonGateway));
        private readonly IMessageQueue _messageQueue;

        public DenonGateway(
            IMessageQueue messageQueue,
            IUpnpDeviceDiscoveringService upnpDeviceDiscoveringService) : base("Denon")
        {
            _messageQueue = messageQueue;

            _actions.Add(new Action("Change Volume") { Fields = { "Volume" } });
            _actions.Add(new Action("Volume Up"));
            _actions.Add(new Action("Volume Down"));
            _actions.Add(new Action("Power On"));
            _actions.Add(new Action("Power Off"));
            _actions.Add(new Action("Mute On"));
            _actions.Add(new Action("Mute Off"));
            _actions.Add(new Action("Change Input Source") { Fields = { "Source" } });

            _canCreateDevices = false;

            upnpDeviceDiscoveringService.DeviceFound += OnUpnpDeviceFound;
        }

        protected override async Task ExecuteInternal(IDevice device, IAction action, IDictionary<string, string> values)
        {
            string command = null;

            switch (action.Name.ToLowerInvariant())
            {
                case "change volume":
                    string volume;
                    int v;
                    if (values.TryGetValue("Volume", out volume) && int.TryParse(volume, out v))
                    {
                        volume = Math.Max(0, Math.Min(98, v)).ToString("D2");
                        command = "MV" + volume;
                    }
                    break;
                case "volume up":
                    command = "MVUP";
                    break;
                case "volume down":
                    command = "MVDOWN";
                    break;
                case "power on":
                    command = "PWON";
                    break;
                case "power off":
                    command = "PWSTANDBY";
                    break;
                case "mute on":
                    command = "MUON";
                    break;
                case "mute off":
                    command = "MUOFF";
                    break;
                case "change input source":
                    string source;
                    if (values.TryGetValue("Source", out source))
                    {
                        command = "SI" + source;
                    }
                    break;
            }

            var denon = device as DenonDevice;

            if (string.IsNullOrEmpty(command) || denon == null)
            {
                return;
            }

            _log.Debug($"Send command {command} to {denon.IpAddress}.");

            using (var client = new TcpClient())
            {
                await client.ConnectAsync(denon.IpAddress, 23);

                using (var stream = client.GetStream())
                {
                    var data = Encoding.UTF8.GetBytes(command + '\r');
                    await stream.WriteAsync(data, 0, data.Length);
                    await stream.FlushAsync();
                }
            }
        }

        private async void OnUpnpDeviceFound(object sender, IUpnpDeviceResponse e)
        {
            if (e.Location.IndexOf(":8080/description.xml", StringComparison.OrdinalIgnoreCase) < 0 &&
                e.Server.IndexOf("knos/", StringComparison.OrdinalIgnoreCase) < 0)
            {
                return;
            }

            await RegisterDevice(e.Location, e.IpAddress);
        }

        private async Task RegisterDevice(string url, string ipAddress)
        {
            var xmlResponse = await new RestClient(url).ExecuteGetTaskAsync<object>(new RestRequest(Method.GET));
            var xml = new XmlDocument();
            xml.LoadXml(xmlResponse.Content);

            var ns = new XmlNamespaceManager(xml.NameTable);
            ns.AddNamespace("n", "urn:schemas-upnp-org:device-1-0");

            var mn = xml.SelectSingleNode("//n:manufacturer", ns);
            var fn = xml.SelectSingleNode("//n:friendlyName", ns);
            var sn = xml.SelectSingleNode("//n:serialNumber", ns);

            if (mn == null || fn == null || sn == null || !"Denon".Equals(mn.InnerText, StringComparison.Ordinal))
            {
                return;
            }

            var client = new RestClient($"http://{ipAddress}/");
            var request = new RestRequest("goform/formMainZone_MainZoneXml.xml", Method.GET);
            var response = await client.ExecuteTaskAsync<DenonDeviceDto>(request);

            var device = new DenonDevice(sn.InnerText, ipAddress, response.Data);
            _devices.Add(device);

            var volume = double.Parse(response.Data.MasterVolume.Value);
            var isMute = response.Data.Mute.Value.Equals("on", StringComparison.OrdinalIgnoreCase);
            var select = response.Data.InputFuncSelect.Value;

            _messageQueue.Publish(new UpdateVariableMessage($"{Name}.{device.Id}.Volume", volume));
            _messageQueue.Publish(new UpdateVariableMessage($"{Name}.{device.Id}.IsMute", isMute));
            _messageQueue.Publish(new UpdateVariableMessage($"{Name}.{device.Id}.Source", select));
            _messageQueue.Publish(new UpdateVariableMessage($"{Name}.{device.Id}.Name", device.Name));
        }
    }
}