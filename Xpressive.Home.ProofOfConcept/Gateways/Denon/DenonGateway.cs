using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using OpenSource.UPnP;
using RestSharp;

namespace Xpressive.Home.ProofOfConcept.Gateways.Denon
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

            CanCreateDevices = false;

            FindDevices();
        }

        public override bool IsConfigurationValid()
        {
            return true;
        }

        protected override async Task ExecuteInternal(DeviceBase device, IAction action, IDictionary<string, string> values)
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

        private async Task FindDevices()
        {
            Parallel.ForEach(_ipAddressService.GetOtherIpAddresses(), async address =>
            {
                var client = new RestClient($"http://{address}/");
                var request = new RestRequest("goform/formMainZone_MainZoneXml.xml", Method.GET);

                var response = await client.ExecuteTaskAsync<DenonDeviceDto>(request);

                if (response.ResponseStatus == ResponseStatus.Completed && response.StatusCode == HttpStatusCode.OK)
                {
                    var name = response.Data.FriendlyName.Value;
                    var ip = address;

                    var device = new DenonDevice(address)
                    {
                        Name = name
                    };

                    _devices.Add(device);
                }
            });
        }
    }

    internal class DenonDevice : DeviceBase
    {
        public DenonDevice(string ipAddress)
        {
            Id = ipAddress;
        }

        public string IpAddress => Id;
    }

    [XmlRoot("item")]
    public class DenonDeviceDto
    {
        [XmlElement("FriendlyName")]
        public ValueDto FriendlyName { get; set; }

        [XmlElement("Power")]
        public ValueDto Power { get; set; }

        [XmlElement("InputFuncSelect")]
        public ValueDto InputFuncSelect { get; set; }

        [XmlElement("BrandId")]
        public ValueDto BrandId { get; set; }

        [XmlElement("MasterVolume")]
        public ValueDto MasterVolume { get; set; }

        [XmlElement("ModelId")]
        public ValueDto ModelId { get; set; }

        [XmlElement("Mute")]
        public ValueDto Mute { get; set; }

        [XmlElement("NetFuncSelect")]
        public ValueDto NetFuncSelect { get; set; }

        [XmlElement("RemoteMaintenance")]
        public ValueDto RemoteMaintenance { get; set; }

        [XmlElement("RenameZone")]
        public ValueDto RenameZone { get; set; }

        [XmlElement("SalesArea")]
        public ValueDto SalesArea { get; set; }

        [XmlElement("SubwooferDisplay")]
        public ValueDto SubwooferDisplay { get; set; }

        [XmlElement("TopMenuLink")]
        public ValueDto TopMenuLink { get; set; }

        [XmlElement("VideoSelect")]
        public ValueDto VideoSelect { get; set; }

        [XmlElement("VideoSelectDisp")]
        public ValueDto VideoSelectDisp { get; set; }

        [XmlElement("VideoSelectOnOff")]
        public ValueDto VideoSelectOnOff { get; set; }

        [XmlElement("VolumeDisplay")]
        public ValueDto VolumeDisplay { get; set; }

        [XmlElement("Zone2VolDisp")]
        public ValueDto Zone2VolDisp { get; set; }

        [XmlElement("ZonePower")]
        public ValueDto ZonePower { get; set; }

        [XmlElement("selectSurround")]
        public ValueDto SelectSurround { get; set; }
    }

    public class ValueDto
    {
        [XmlElement("value")]
        public string Value { get; set; }
    }
}
