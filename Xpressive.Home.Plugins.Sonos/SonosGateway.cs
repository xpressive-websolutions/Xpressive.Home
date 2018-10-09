using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Serilog;
using Xpressive.Home.Contracts.Gateway;
using Xpressive.Home.Contracts.Messaging;
using Action = Xpressive.Home.Contracts.Gateway.Action;

namespace Xpressive.Home.Plugins.Sonos
{
    internal class SonosGateway : GatewayBase, ISonosGateway
    {
        private readonly IMessageQueue _messageQueue;
        private readonly ISonosSoapClient _soapClient;

        public SonosGateway(IMessageQueue messageQueue, ISonosDeviceDiscoverer deviceDiscoverer, ISonosSoapClient soapClient) : base("Sonos")
        {
            _messageQueue = messageQueue;
            _soapClient = soapClient;
            _canCreateDevices = false;

            deviceDiscoverer.DeviceFound += (s, e) =>
            {
                e.Id = e.Id.Replace("uuid:", string.Empty);

                if (!string.IsNullOrEmpty(e.Type))
                {
                    e.Icon = "SonosIcon SonosIcon_" + e.Type;
                }

                _devices.TryAdd(e.Id, e);
            };
        }

        public override IDevice CreateEmptyDevice()
        {
            throw new NotSupportedException();
        }

        public IEnumerable<SonosDevice> GetDevices()
        {
            return Devices.OfType<SonosDevice>();
        }

        public override IEnumerable<IAction> GetActions(IDevice device)
        {
            if (device is SonosDevice)
            {
                yield return new Action("Play");
                yield return new Action("Pause");
                yield return new Action("Stop");
                yield return new Action("Play Radio") { Fields = { "Stream", "Title" } };
                yield return new Action("Play File") { Fields = { "File", "Title", "Album" } };
                yield return new Action("Change Volume") { Fields = { "Volume" } };
            }
        }

        public void Play(SonosDevice device)
        {
            StartActionInNewTask(device, new Action("Play"), new Dictionary<string, string>(0));
        }

        public void Pause(SonosDevice device)
        {
            StartActionInNewTask(device, new Action("Pause"), new Dictionary<string, string>(0));
        }

        public void Stop(SonosDevice device)
        {
            StartActionInNewTask(device, new Action("Stop"), new Dictionary<string, string>(0));
        }

        public void PlayRadio(SonosDevice device, string stream, string title)
        {
            var parameters = new Dictionary<string, string>
            {
                {"Stream", stream},
                {"Title", title}
            };

            StartActionInNewTask(device, new Action("Play Radio"), parameters);
        }

        public void PlayFile(SonosDevice device, string file, string title, string album)
        {
            var parameters = new Dictionary<string, string>
            {
                {"File", file},
                {"Title", title},
                {"Album", album}
            };

            StartActionInNewTask(device, new Action("Play File"), parameters);
        }

        public void ChangeVolume(SonosDevice device, double volume)
        {
            var parameters = new Dictionary<string, string>
            {
                {"Volume", volume.ToString("F2")}
            };

            StartActionInNewTask(device, new Action("Change Volume"), parameters);
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ContinueWith(_ => { });

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var devices = GetDevices().ToList();
                    var masterDevices = devices.Where(d => d.IsMaster).ToList();
                    var others = devices.Except(masterDevices).ToList();

                    masterDevices.ForEach(async d => await UpdateDeviceVariablesAsync(d));
                    others.ForEach(async d => await UpdateDeviceVariablesAsync(d));
                }
                catch (Exception e)
                {
                    Log.Error(e, e.Message);
                }

                await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken).ContinueWith(_ => { });
            }
        }

        protected override async Task ExecuteInternalAsync(IDevice device, IAction action, IDictionary<string, string> values)
        {
            if (device == null)
            {
                Log.Warning("Unable to execute action {actionName} because the device was not found.", action.Name);
                return;
            }

            var d = device as SonosDevice;
            string stream;
            string file;
            string title;
            string album;
            string volume;

            values.TryGetValue("Stream", out stream);
            values.TryGetValue("File", out file);
            values.TryGetValue("Title", out title);
            values.TryGetValue("Album", out album);
            values.TryGetValue("Volume", out volume);

            if (d == null)
            {
                return;
            }

            var master = GetMaster(d);

            switch (action.Name.ToLowerInvariant())
            {
                case "play":
                    await SendAvTransportControl(master, "Play");
                    break;
                case "pause":
                    await SendAvTransportControl(master, "Pause");
                    break;
                case "stop":
                    await SendAvTransportControl(master, "Stop");
                    break;
                case "play radio":
                    if (!string.IsNullOrEmpty(stream) && !string.IsNullOrEmpty(title))
                    {
                        var metadata = GetRadioMetadata(title);
                        var url = ReplaceScheme(stream, "x-rincon-mp3radio");
                        await SendUrl(master, url, metadata);
                        await SendAvTransportControl(master, "Play");
                    }
                    break;
                case "play file":
                    if (!string.IsNullOrEmpty(file))
                    {
                        var metadata = GetFileMetadata(file, album);
                        var url = ReplaceScheme(file, "x-file-cifs");
                        await SendUrl(master, url, metadata);
                        await SendAvTransportControl(master, "Play");
                    }
                    break;
                case "change volume":
                    var v = Math.Min(Math.Max((int)(100 * double.Parse(volume)), 0), 100);
                    var rc = d.Services.Single(s => s.Id.Contains(":RenderingControl"));
                    var sv = rc.Actions.Single(s => s.Name.Equals("SetVolume"));
                    var parameters = new Dictionary<string, string>
                    {
                        {"InstanceID" , "0"},
                        {"Channel" , "Master"},
                        {"DesiredVolume" , v.ToString("D")}
                    };
                    await _soapClient.ExecuteAsync(d, rc, sv, parameters);
                    break;
            }
        }

        private async Task UpdateDeviceVariablesAsync(SonosDevice device)
        {
            var avTransport = device.Services.Single(s => s.Id.Contains("AVTransport"));
            var renderingControl = device.Services.Single(s => s.Id.Contains(":RenderingControl"));
            var deviceProperties = device.Services.Single(s => s.Id.Contains("DeviceProperties"));
            var getMediaInfo = avTransport.Actions.Single(s => s.Name.Equals("GetMediaInfo"));
            var getTransportInfo = avTransport.Actions.Single(s => s.Name.Equals("GetTransportInfo"));
            var getPositionInfo = avTransport.Actions.Single(s => s.Name.Equals("GetPositionInfo"));
            var getVolume = renderingControl.Actions.Single(s => s.Name.Equals("GetVolume"));
            var getZoneAttributes = deviceProperties.Actions.Single(s => s.Name.Equals("GetZoneAttributes"));

            var values = new Dictionary<string, string>
            {
                {"InstanceID", "0"}
            };

            var mediaInfo = await _soapClient.ExecuteAsync(device, avTransport, getMediaInfo, values);
            var transportInfo = await _soapClient.ExecuteAsync(device, avTransport, getTransportInfo, values);
            var positionInfo = await _soapClient.ExecuteAsync(device, avTransport, getPositionInfo, values);

            values.Add("Channel", "Master");
            var volume = await _soapClient.ExecuteAsync(device, renderingControl, getVolume, values);

            values.Clear();
            var zoneAttributes = await _soapClient.ExecuteAsync(device, deviceProperties, getZoneAttributes, values);

            string currentUri;
            string metadata;
            string transportState;
            string currentVolume;
            if (!mediaInfo.TryGetValue("CurrentURI", out currentUri) ||
                !mediaInfo.TryGetValue("CurrentURIMetaData", out metadata) ||
                !transportInfo.TryGetValue("CurrentTransportState", out transportState) ||
                !volume.TryGetValue("CurrentVolume", out currentVolume))
            {
                return;
            }

            string trackUri;
            positionInfo.TryGetValue("TrackURI", out trackUri);

            transportState = transportState[0] + transportState.Substring(1).ToLowerInvariant();

            device.CurrentUri = currentUri;
            device.TransportState = transportState;
            device.Volume = double.Parse(currentVolume);
            device.IsMaster = string.IsNullOrEmpty(trackUri) || !trackUri.StartsWith("x-rincon:RINCON");
            device.Zone = zoneAttributes["CurrentZoneName"] ?? string.Empty;

            var master = GetMaster(device);
            if (!ReferenceEquals(master, device))
            {
                device.TransportState = master.TransportState;
            }

            UpdateVariable(device, "TransportState", device.TransportState);
            UpdateVariable(device, "CurrentUri", device.CurrentUri);
            UpdateVariable(device, "Volume", device.Volume);
            UpdateVariable(device, "IsMaster", device.IsMaster);
            UpdateVariable(device, "Zone", device.Zone);
        }

        private SonosDevice GetMaster(SonosDevice device)
        {
            if (device.IsMaster)
            {
                return device;
            }

            var masterId = device.CurrentUri?.Replace("x-rincon:", string.Empty) ?? string.Empty;
            var master = (SonosDevice)Devices.SingleOrDefault(d => d.Id.Equals(masterId, StringComparison.OrdinalIgnoreCase));
            return master ?? device;
        }

        private string ReplaceScheme(string url, string scheme)
        {
            var uri = new Uri(url);

            if (uri.Scheme.StartsWith("x-", StringComparison.OrdinalIgnoreCase))
            {
                return url;
            }

            var path = HttpUtility.UrlDecode(uri.PathAndQuery);
            return $"{scheme}://{uri.Authority}{path}";
        }

        private async Task SendUrl(SonosDevice device, string url, string metadata)
        {
            metadata = HttpUtility.HtmlEncode(metadata);
            url = HttpUtility.HtmlEncode(url);

            var service = device.Services.Single(s => s.Id.Contains("AVTransport"));
            var action = service.Actions.Single(s => s.Name.Equals("SetAVTransportURI"));
            var values = new Dictionary<string, string>
            {
                {"InstanceID", "0"},
                {"CurrentURI", url},
                {"CurrentURIMetaData", metadata}
            };

            await _soapClient.ExecuteAsync(device, service, action, values);
        }

        private async Task SendAvTransportControl(SonosDevice device, string command)
        {
            var service = device.Services.Single(s => s.Id.Contains("AVTransport"));
            var action = service.Actions.Single(s => s.Name.Equals(command));
            var values = new Dictionary<string, string>
            {
                {"InstanceID", "0"},
                {"Speed", "1"}

            };

            await _soapClient.ExecuteAsync(device, service, action, values);
        }

        private string GetRadioMetadata(string title)
        {
            var didl = $"<DIDL-Lite xmlns:dc=\"http://purl.org/dc/elements/1.1/\" xmlns:upnp=\"urn:schemas-upnp-org:metadata-1-0/upnp/\" xmlns:r=\"urn:schemas-rinconnetworks-com:metadata-1-0/\" xmlns=\"urn:schemas-upnp-org:metadata-1-0/DIDL-Lite/\"><item id=\"R:0/0/0\" parentID=\"R:0/0\" restricted=\"true\"><dc:title>{title}</dc:title><upnp:class>object.item.audioItem.audioBroadcast</upnp:class><desc id=\"cdudn\" nameSpace=\"urn:schemas-rinconnetworks-com:metadata-1-0/\">SA_RINCON65031_</desc></item></DIDL-Lite>";
            return didl;
        }

        private string GetFileMetadata(string title, string album)
        {
            return null;
        }

        private void UpdateVariable(SonosDevice device, string variable, object value)
        {
            _messageQueue.Publish(new UpdateVariableMessage(Name, device.Id, variable, value));
        }
    }
}
