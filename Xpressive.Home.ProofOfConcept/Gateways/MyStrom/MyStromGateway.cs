using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using RestSharp;

namespace Xpressive.Home.ProofOfConcept.Gateways.MyStrom
{
    internal class MyStromGateway : GatewayBase
    {
        private readonly IIpAddressService _ipAddressService;
        private readonly IMessageQueue _messageQueue;
        private readonly IVariableRepository _variableRepository;
        private readonly object _deviceListLock = new object();

        public MyStromGateway(IIpAddressService ipAddressService, IMessageQueue messageQueue, IVariableRepository variableRepository) : base("myStrom")
        {
            _ipAddressService = ipAddressService;
            _messageQueue = messageQueue;
            _variableRepository = variableRepository;

            _actions.Add(new Action("Switch On"));
            _actions.Add(new Action("Switch Off"));

            _properties.Add(new NumericProperty("Power", double.MinValue, double.MaxValue, isReadOnly: true));
            _properties.Add(new BoolProperty("Relay", isReadOnly: true));

            Observe();
            FindDevices();
        }

        public override bool IsConfigurationValid()
        {
            return true;
        }

        protected override async Task ExecuteInternal(DeviceBase device, IAction action, IDictionary<string, string> values)
        {
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

        private async Task FindDevices()
        {
            while (true)
            {
                var addresses = _ipAddressService.GetOtherIpAddresses();

                Parallel.ForEach(addresses, async address =>
                {
                    var dto = await GetReport(address);

                    if (dto != null)
                    {
                        await RegisterDevice(address);
                    }
                });

                await Task.Delay(TimeSpan.FromMinutes(1));
            }
        }

        private async Task RegisterDevice(string ipAddress)
        {
            var client = new RestClient($"http://{ipAddress}/");
            var request = new RestRequest("info.json", Method.GET);
            var response = await client.ExecuteTaskAsync<MyStromDeviceInfo>(request);

            if (response.Data == null)
            {
                return;
            }

            lock (_deviceListLock)
            {
                if (_devices.Any(d => d.Id.Equals(ipAddress)))
                {
                    return;
                }

                Console.WriteLine($"Found myStrom device {ipAddress} - {response.Data.Mac}");

                _variableRepository.Register(new DoubleVariable($"{Name}.mystromdevicename.Power"));
                _variableRepository.Register(new BooleanVariable($"{Name}.mystromdevicename.Relay"));
                _devices.Add(new MyStromDevice("mystromdevicename", ipAddress, response.Data.Mac));
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

        private async Task Observe()
        {
            var powerProperty = GetProperty("Power");
            var relayProperty = GetProperty("Relay");

            var previousPowers = new Dictionary<string, double>();

            while (true)
            {
                await Task.Delay(TimeSpan.FromSeconds(1));

                foreach (MyStromDevice device in _devices)
                {
                    var dto = await GetReport(device.IpAddress);

                    if (dto == null)
                    {
                        device.ReadStatus = DeviceReadStatus.Erroneous;
                        continue;
                    }

                    _messageQueue.Publish(new UpdateVariableMessage(Name, device.Name, "Relay", dto.Relay));

                    double previousPower;
                    if (previousPowers.TryGetValue(device.Id, out previousPower))
                    {
                        if (Math.Abs(previousPower - dto.Power) > 0.01)
                        {
                            _messageQueue.Publish(new UpdateVariableMessage(Name, device.Name, "Power", dto.Power));
                            previousPowers[device.Id] = dto.Power;
                        }
                    }
                    else
                    {
                        previousPowers[device.Id] = dto.Power;
                    }
                }
            }
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
    }
}