using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using log4net;
using RestSharp;
using Xpressive.Home.Contracts;
using Xpressive.Home.Contracts.Gateway;
using Xpressive.Home.Contracts.Messaging;
using Xpressive.Home.Contracts.Services;
using Action = Xpressive.Home.Contracts.Gateway.Action;

namespace Xpressive.Home.Plugins.Denon
{
    internal class DenonGateway : GatewayBase, IDenonGateway
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof (DenonGateway));
        private readonly IMessageQueue _messageQueue;
        private readonly IUpnpDeviceDiscoveringService _upnpDeviceDiscoveringService;
        private bool _isRunning;

        public DenonGateway(
            IMessageQueue messageQueue,
            IUpnpDeviceDiscoveringService upnpDeviceDiscoveringService) : base("Denon")
        {
            _messageQueue = messageQueue;
            _upnpDeviceDiscoveringService = upnpDeviceDiscoveringService;

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

        public override IDevice CreateEmptyDevice()
        {
            throw new NotSupportedException();
        }

        public IEnumerable<DenonDevice> GetDevices()
        {
            return Devices.OfType<DenonDevice>();
        }

        public async void PowerOn(DenonDevice device)
        {
            await ExecuteInternal(device, new Action("Power On"), null);
        }

        public async void PowerOff(DenonDevice device)
        {
            await ExecuteInternal(device, new Action("Power Off"), null);
        }

        public async void ChangeVolumne(DenonDevice device, int volume)
        {
            var parameters = new Dictionary<string, string>
            {
                {"Volume", volume.ToString()}
            };

            await ExecuteInternal(device, new Action("Change Volume"), parameters);
        }

        public async void Mute(DenonDevice device)
        {
            await ExecuteInternal(device, new Action("Mute On"), null);
        }

        public async void Unmute(DenonDevice device)
        {
            await ExecuteInternal(device, new Action("Mute Off"), null);
        }

        public async void ChangeInput(DenonDevice device, string source)
        {
            var parameters = new Dictionary<string, string>
            {
                {"Source", source}
            };

            await ExecuteInternal(device, new Action("Change Input Source"), parameters);
        }

        protected override async Task ExecuteInternal(IDevice device, IAction action, IDictionary<string, string> values)
        {
            if (device == null)
            {
                _log.Warn($"Unable to execute action {action.Name} because the device was not found.");
                return;
            }

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

        public override async Task StartAsync()
        {
            _isRunning = true;

            while (_isRunning)
            {
                await TaskHelper.DelayAsync(TimeSpan.FromMinutes(1), () => _isRunning);

                foreach (var device in GetDevices())
                {
                    if (_isRunning)
                    {
                        await UpdateVariablesAsync(device);
                    }
                }
            }
        }

        public override void Stop()
        {
            _isRunning = false;
            _upnpDeviceDiscoveringService.DeviceFound -= OnUpnpDeviceFound;
        }

        private async void OnUpnpDeviceFound(object sender, IUpnpDeviceResponse e)
        {
            if (e.Location.IndexOf(":8080/description.xml", StringComparison.OrdinalIgnoreCase) < 0 &&
                e.Server.IndexOf("knos/", StringComparison.OrdinalIgnoreCase) < 0)
            {
                return;
            }

            var device = await RegisterDeviceAsync(e.Location, e.IpAddress);
            await UpdateVariablesAsync(device);
        }

        private async Task<DenonDevice> RegisterDeviceAsync(string url, string ipAddress)
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
                return null;
            }

            var device = new DenonDevice(sn.InnerText, ipAddress);
            _devices.Add(device);
            return device;
        }

        private async Task UpdateVariablesAsync(DenonDevice device)
        {
            var client = new RestClient($"http://{device.IpAddress}/");
            var request = new RestRequest("goform/formMainZone_MainZoneXml.xml", Method.GET);
            var response = await client.ExecuteTaskAsync<DenonDeviceDto>(request);

            var volume = double.Parse(response.Data.MasterVolume.Value);
            var isMute = response.Data.Mute.Value.Equals("on", StringComparison.OrdinalIgnoreCase);
            var select = response.Data.InputFuncSelect.Value;
            device.Name = response.Data.FriendlyName.Value;
            device.Volume = volume;
            device.IsMute = isMute;
            device.Source = select;

            _messageQueue.Publish(new UpdateVariableMessage($"{Name}.{device.Id}.Volume", volume));
            _messageQueue.Publish(new UpdateVariableMessage($"{Name}.{device.Id}.IsMute", isMute));
            _messageQueue.Publish(new UpdateVariableMessage($"{Name}.{device.Id}.Source", select));
            _messageQueue.Publish(new UpdateVariableMessage($"{Name}.{device.Id}.Name", device.Name));
        }
    }
}
