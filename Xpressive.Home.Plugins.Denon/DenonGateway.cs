using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using RestSharp;
using Xpressive.Home.Contracts.Gateway;
using Xpressive.Home.Contracts.Services;
using Action = Xpressive.Home.Contracts.Gateway.Action;

namespace Xpressive.Home.Plugins.Denon
{
    internal class DenonGateway : GatewayBase
    {
        private readonly IIpAddressService _ipAddressService;

        public DenonGateway(IIpAddressService ipAddressService) : base("Denon")
        {
            _ipAddressService = ipAddressService;

            _actions.Add(new Action("Change Volume") { Fields = { "Volume" } });
            _actions.Add(new Action("Volume Up"));
            _actions.Add(new Action("Volume Down"));
            _actions.Add(new Action("Power On"));
            _actions.Add(new Action("Power Off"));
            _actions.Add(new Action("Mute On"));
            _actions.Add(new Action("Mute Off"));
            _actions.Add(new Action("Change Input Source") { Fields = { "Source" } });

            _canCreateDevices = false;

            FindDevices();
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

            Console.WriteLine($"Send command {command} to {denon.IpAddress}.");

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

        private void FindDevices()
        {
            Parallel.ForEach(_ipAddressService.GetOtherIpAddresses(), async address =>
            {
                var client = new RestClient($"http://{address}/");
                var request = new RestRequest("goform/formMainZone_MainZoneXml.xml", Method.GET);

                var response = await client.ExecuteTaskAsync<DenonDeviceDto>(request);

                if (response.ResponseStatus == ResponseStatus.Completed && response.StatusCode == HttpStatusCode.OK)
                {
                    var device = new DenonDevice(address, response.Data);
                    _devices.Add(device);
                }
            });
        }
    }
}