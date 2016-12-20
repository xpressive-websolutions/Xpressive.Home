using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using log4net;
using Polly;
using RestSharp;
using Xpressive.Home.Contracts.Gateway;
using Xpressive.Home.Contracts.Messaging;
using Xpressive.Home.Contracts.Services;
using Action = Xpressive.Home.Contracts.Gateway.Action;

namespace Xpressive.Home.Plugins.Denon
{
    internal class DenonGateway : GatewayBase, IDenonGateway
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(DenonGateway));
        private readonly IMessageQueue _messageQueue;
        private readonly IUpnpDeviceDiscoveringService _upnpDeviceDiscoveringService;
        private readonly object _deviceLock = new object();

        public DenonGateway(
            IMessageQueue messageQueue,
            IUpnpDeviceDiscoveringService upnpDeviceDiscoveringService) : base("Denon")
        {
            _messageQueue = messageQueue;
            _upnpDeviceDiscoveringService = upnpDeviceDiscoveringService;
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

        public override IEnumerable<IAction> GetActions(IDevice device)
        {
            if (device is DenonDevice)
            {
                yield return new Action("Change Volume") { Fields = { "Volume" } };
                yield return new Action("Volume Up");
                yield return new Action("Volume Down");
                yield return new Action("Power On");
                yield return new Action("Power Off");
                yield return new Action("Mute On");
                yield return new Action("Mute Off");
                yield return new Action("Change Input Source") { Fields = { "Source" } };
            }
        }

        public void PowerOn(DenonDevice device)
        {
            StartActionInNewTask(device, new Action("Power On"), null);
        }

        public void PowerOff(DenonDevice device)
        {
            StartActionInNewTask(device, new Action("Power Off"), null);
        }

        public void ChangeVolumne(DenonDevice device, int volume)
        {
            var parameters = new Dictionary<string, string>
            {
                {"Volume", volume.ToString()}
            };

            StartActionInNewTask(device, new Action("Change Volume"), parameters);
        }

        public void Mute(DenonDevice device)
        {
            StartActionInNewTask(device, new Action("Mute On"), null);
        }

        public void Unmute(DenonDevice device)
        {
            StartActionInNewTask(device, new Action("Mute Off"), null);
        }

        public void ChangeInput(DenonDevice device, string source)
        {
            var parameters = new Dictionary<string, string>
            {
                {"Source", source}
            };

            StartActionInNewTask(device, new Action("Change Input Source"), parameters);
        }

        protected override async Task ExecuteInternalAsync(IDevice device, IAction action, IDictionary<string, string> values)
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

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ContinueWith(_ => { });

            while (!cancellationToken.IsCancellationRequested)
            {
                foreach (var device in GetDevices())
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await UpdateVariablesAsync(device);
                    }
                }

                await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken).ContinueWith(_ => { });
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _upnpDeviceDiscoveringService.DeviceFound -= OnUpnpDeviceFound;
            }
            base.Dispose(disposing);
        }

        private async void OnUpnpDeviceFound(object sender, IUpnpDeviceResponse e)
        {
            if (e.Location.IndexOf(":8080/description.xml", StringComparison.OrdinalIgnoreCase) < 0 &&
                e.Server.IndexOf("knos/", StringComparison.OrdinalIgnoreCase) < 0)
            {
                return;
            }

            var policy = Policy
                .Handle<WebException>()
                .Or<XmlException>()
                .WaitAndRetryAsync(new[]
                {
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(2),
                    TimeSpan.FromSeconds(5)
                });

            await policy.ExecuteAsync(async () =>
            {
                var device = RegisterDevice(e.Location, e.IpAddress);

                if (device == null)
                {
                    return;
                }

                await UpdateVariablesAsync(device);
            });
        }

        private DenonDevice RegisterDevice(string url, string ipAddress)
        {
            var xml = new XmlDocument();
            xml.Load(url);

            var ns = new XmlNamespaceManager(xml.NameTable);
            ns.AddNamespace("n", "urn:schemas-upnp-org:device-1-0");

            var mn = xml.SelectSingleNode("//n:manufacturer", ns);
            var fn = xml.SelectSingleNode("//n:friendlyName", ns);
            var sn = xml.SelectSingleNode("//n:serialNumber", ns);

            if (mn == null || fn == null || sn == null || !"Denon".Equals(mn.InnerText, StringComparison.Ordinal))
            {
                return null;
            }

            lock (_deviceLock)
            {
                if (GetDevices().Any(d => d.IpAddress.Equals(ipAddress, StringComparison.OrdinalIgnoreCase)))
                {
                    return null;
                }

                var device = new DenonDevice(sn.InnerText, ipAddress)
                {
                    Name = fn.InnerText.Replace("Denon", string.Empty).Trim()
                };
                _devices.Add(device);

                return device;
            }

        }

        private async Task UpdateVariablesAsync(DenonDevice device)
        {
            var client = new RestClient($"http://{device.IpAddress}/");
            var request = new RestRequest("goform/formMainZone_MainZoneXml.xml", Method.GET);
            var response = await client.ExecuteTaskAsync<DenonDeviceDto>(request);

            double volume;
            var isMute = response.Data.Mute.Value.Equals("on", StringComparison.OrdinalIgnoreCase);
            var select = response.Data.InputFuncSelect.Value;

            if (!double.TryParse(response.Data.MasterVolume.Value, out volume))
            {
                volume = 0;
            }

            device.Name = response.Data.FriendlyName.Value.Replace("Denon", string.Empty).Trim();
            device.Volume = volume;
            device.IsMute = isMute;
            device.Source = select;

            _messageQueue.Publish(new UpdateVariableMessage($"{Name}.{device.Id}.Volume", volume));
            _messageQueue.Publish(new UpdateVariableMessage($"{Name}.{device.Id}.IsMute", isMute));
            _messageQueue.Publish(new UpdateVariableMessage($"{Name}.{device.Id}.Source", select));
        }
    }
}
