using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

using RestSharp;

namespace Xpressive.Home.ProofOfConcept.Gateways.MyStrom
{
    internal class MyStromGateway : GatewayBase
    {
        private readonly object _deviceListLock = new object();

        public MyStromGateway() : base("myStrom")
        {
            _actions.Add(new Action("Switch On"));
            _actions.Add(new Action("Switch Off"));

            _properties.Add("Power");
            _properties.Add("Relay");

            Observe();
            FindDevices();
        }

        protected override async Task<string> GetInternal(IDevice device, string property)
        {
            var dto = await GetReport(((MyStromDevice)device).IpAddress);

            if (dto == null)
            {
                return null;
            }

            switch (property.ToLowerInvariant())
            {
                case "power":
                    return dto.Power.ToString("F6");
                case "relay":
                    return dto.Relay.ToString();
            }

            return null;
        }

        protected override async Task ExecuteInternal(IDevice device, IAction action, IDictionary<string, string> values)
        {
            var d = (MyStromDevice)device;
            var client = new RestClient("http://" + d.IpAddress);
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
                var ipAddress = GetIpAddress();
                var addresses = GetOtherIpAddresses(ipAddress);

                Parallel.ForEach(addresses, async address =>
                {
                    var dto = await GetReport(address);

                    if (dto != null)
                    {
                        RegisterDevice(address);
                    }
                });

                await Task.Delay(TimeSpan.FromMinutes(1));
            }
        }

        private void RegisterDevice(string ipAddress)
        {
            lock (_deviceListLock)
            {
                if (_devices.Any(d => d.Id.Equals(ipAddress)))
                {
                    return;
                }

                Console.WriteLine("Found myStrom device {0}", ipAddress);
                _devices.Add(new MyStromDevice(ipAddress));
            }
        }

        private async Task<Dto> GetReport(string ipAddress)
        {
            var client = new RestClient("http://" + ipAddress);
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
            var powerProperty = Properties.Single(p => p.Equals("Power", StringComparison.Ordinal));
            var relayProperty = Properties.Single(p => p.Equals("Relay", StringComparison.Ordinal));

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

                    OnDevicePropertyChanged(device, relayProperty, dto.Relay.ToString());

                    double previousPower;
                    if (previousPowers.TryGetValue(device.Id, out previousPower))
                    {
                        if (Math.Abs(previousPower - dto.Power) > 0.01)
                        {
                            OnDevicePropertyChanged(device, powerProperty, dto.Power.ToString("F6"));
                        }
                    }

                    previousPowers[device.Id] = dto.Power;
                }
            }
        }

        private IEnumerable<string> GetOtherIpAddresses(string ipAddress)
        {
            var parts = ipAddress.Split('.');
            var prefix = string.Join(".", parts.Take(3));

            for (var i = 0; i < 256; i++)
            {
                yield return $"{prefix}.{i}";
            }
        }

        private string GetIpAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());

            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }

            return string.Empty;
        }

        private class Dto
        {
            public double Power { get; set; }
            public bool Relay { get; set; }
        }
    }
}