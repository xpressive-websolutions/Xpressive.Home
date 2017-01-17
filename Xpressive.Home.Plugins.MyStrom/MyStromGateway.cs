using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Polly;
using RestSharp;
using Xpressive.Home.Contracts.Gateway;
using Xpressive.Home.Contracts.Messaging;
using Xpressive.Home.Contracts.Services;
using Action = Xpressive.Home.Contracts.Gateway.Action;

namespace Xpressive.Home.Plugins.MyStrom
{
    internal class MyStromGateway : GatewayBase, IMyStromGateway
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(MyStromGateway));
        private readonly IMessageQueue _messageQueue;
        private readonly IMyStromDeviceNameService _myStromDeviceNameService;
        private readonly IUpnpDeviceDiscoveringService _upnpDeviceDiscoveringService;
        private readonly IDeviceConfigurationBackupService _deviceConfigurationBackupService;
        private readonly object _deviceListLock = new object();

        public MyStromGateway(
            IMessageQueue messageQueue,
            IMyStromDeviceNameService myStromDeviceNameService,
            IUpnpDeviceDiscoveringService upnpDeviceDiscoveringService,
            IDeviceConfigurationBackupService deviceConfigurationBackupService) : base("myStrom")
        {
            _messageQueue = messageQueue;
            _myStromDeviceNameService = myStromDeviceNameService;
            _upnpDeviceDiscoveringService = upnpDeviceDiscoveringService;
            _deviceConfigurationBackupService = deviceConfigurationBackupService;
            _canCreateDevices = false;

            _upnpDeviceDiscoveringService.DeviceFound += OnUpnpDeviceFound;
        }

        public IEnumerable<MyStromDevice> GetDevices()
        {
            return _devices.Cast<MyStromDevice>();
        }

        public void SwitchOn(MyStromDevice device)
        {
            StartActionInNewTask(device, new Action("Switch On"), null);
        }

        public void SwitchOff(MyStromDevice device)
        {
            StartActionInNewTask(device, new Action("Switch Off"), null);
        }

        public override IEnumerable<IAction> GetActions(IDevice device)
        {
            if (device is MyStromDevice)
            {
                yield return new Action("Switch On");
                yield return new Action("Switch Off");
            }
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            await LoadDevicesFromBackup();

            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ContinueWith(_ => { });

            var previousPowers = new Dictionary<string, double>();

            while (!cancellationToken.IsCancellationRequested)
            {
                foreach (var device in _devices.Cast<MyStromDevice>())
                {
                    var dto = await GetReport(device.IpAddress);

                    if (dto == null)
                    {
                        device.ReadStatus = DeviceReadStatus.Erroneous;
                        continue;
                    }

                    device.ReadStatus = DeviceReadStatus.Ok;
                    device.Power = dto.Power;
                    device.Relay = dto.Relay;
                    _messageQueue.Publish(new UpdateVariableMessage(Name, device.Id, "Relay", dto.Relay));
                    _messageQueue.Publish(new UpdateVariableMessage(Name, device.Id, "Name", device.Name));

                    double previousPower;
                    if (previousPowers.TryGetValue(device.Id, out previousPower))
                    {
                        if (Math.Abs(previousPower - dto.Power) > 0.01)
                        {
                            _messageQueue.Publish(new UpdateVariableMessage(Name, device.Id, "Power", dto.Power));
                            previousPowers[device.Id] = dto.Power;
                        }
                    }
                    else
                    {
                        _messageQueue.Publish(new UpdateVariableMessage(Name, device.Id, "Power", dto.Power));
                        previousPowers[device.Id] = dto.Power;
                    }
                }

                await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken).ContinueWith(_ => { });
            }
        }

        public override IDevice CreateEmptyDevice()
        {
            throw new NotSupportedException();
        }

        protected override async Task ExecuteInternalAsync(IDevice device, IAction action, IDictionary<string, string> values)
        {
            if (device == null)
            {
                _log.Warn($"Unable to execute action {action.Name} because the device was not found.");
                return;
            }

            var d = (MyStromDevice)device;
            var client = new RestClient($"http://{d.IpAddress}");
            var request = new RestRequest("relay", Method.GET);

            switch (action.Name.ToLowerInvariant())
            {
                case "switch on":
                    request.AddQueryParameter("state", "1");
                    break;
                case "switch off":
                    request.AddQueryParameter("state", "0");
                    break;
                default:
                    throw new NotSupportedException(action.Name);
            }

            await client.ExecuteTaskAsync(request);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _upnpDeviceDiscoveringService.DeviceFound -= OnUpnpDeviceFound;
                var ipAddresses = _devices.OfType<MyStromDevice>().Select(d => d.IpAddress).ToList();
                _deviceConfigurationBackupService.Save(Name, new DeviceConfigurationBackupDto(ipAddresses));
            }
            base.Dispose(disposing);
        }

        private async void OnUpnpDeviceFound(object sender, IUpnpDeviceResponse e)
        {
            if (e.Usn.IndexOf("wifi-switch", StringComparison.OrdinalIgnoreCase) < 0 &&
                !e.FriendlyName.Equals("mystrom wifi switch", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            await RegisterDeviceWithRetry(e.IpAddress);
        }

        private async Task LoadDevicesFromBackup()
        {
            var backup = _deviceConfigurationBackupService.Get<DeviceConfigurationBackupDto>(Name);
            if (backup != null)
            {
                foreach (var ipAddress in backup.IpAddresses)
                {
                    await RegisterDeviceWithRetry(ipAddress);
                }
            }
        }

        private async Task RegisterDeviceWithRetry(string ipAddress)
        {
            await Policy
                .Handle<WebException>()
                .OrResult(false)
                .WaitAndRetryAsync(new[]
                {
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(2),
                    TimeSpan.FromSeconds(5),
                })
                .ExecuteAsync(async () => await RegisterDevice(ipAddress));
        }

        private async Task<bool> RegisterDevice(string ipAddress)
        {
            var client = new RestClient($"http://{ipAddress}/");
            var request = new RestRequest("info.json", Method.GET);
            var response = await client.ExecuteTaskAsync<MyStromDeviceInfo>(request);

            if (response.Data == null)
            {
                return false;
            }

            await AddDeviceAsync(null, ipAddress, response.Data.Mac);

            return true;
        }

        private async Task AddDeviceAsync(string name, string ipAddress, string macAddress)
        {
            var namesByMacAddress = await _myStromDeviceNameService.GetDeviceNamesByMacAsync();

            lock (_deviceListLock)
            {
                if (_devices.Cast<MyStromDevice>().Any(d => d.MacAddress.Equals(macAddress)))
                {
                    return;
                }

                if (string.IsNullOrEmpty(name))
                {
                    if (!namesByMacAddress.TryGetValue(macAddress, out name))
                    {
                        name = ipAddress;
                    }
                }

                _devices.Add(new MyStromDevice(name, ipAddress, macAddress));
            }
        }

        private async Task<Dto> GetReport(string ipAddress)
        {
            var client = new RestClient($"http://{ipAddress}");
            client.Timeout = 5000;

            var response = await client.ExecuteTaskAsync<Dto>(new RestRequest("report", Method.GET));

            if (response.ResponseStatus == ResponseStatus.Completed &&
                response.StatusCode == HttpStatusCode.OK &&
                response.Data != null)
            {
                return response.Data;
            }

            return null;
        }

        private class Dto
        {
            public double Power { get; set; }
            public bool Relay { get; set; }
        }

        private class MyStromDeviceInfo
        {
            public string Version { get; set; }
            public string Mac { get; set; }
        }

        private class DeviceConfigurationBackupDto
        {
            public DeviceConfigurationBackupDto(IEnumerable<string> ipAddresses)
            {
                IpAddresses = new List<string>(ipAddresses);
            }

            public List<string> IpAddresses { get; }
        }
    }
}
