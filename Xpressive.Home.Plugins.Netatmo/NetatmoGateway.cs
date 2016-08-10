using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using log4net;
using RestSharp;
using Xpressive.Home.Contracts.Gateway;
using Xpressive.Home.Contracts.Messaging;

namespace Xpressive.Home.Plugins.Netatmo
{
    internal class NetatmoGateway : GatewayBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof (NetatmoGateway));
        private readonly IMessageQueue _messageQueue;
        private readonly object _deviceLock = new object();
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly string _username;
        private readonly string _password;
        private readonly bool _isValidConfiguration;

        public NetatmoGateway(IMessageQueue messageQueue) : base("Netatmo")
        {
            _messageQueue = messageQueue;
            _clientId = ConfigurationManager.AppSettings["netatmo.clientid"];
            _clientSecret = ConfigurationManager.AppSettings["netatmo.clientsecret"];
            _username = ConfigurationManager.AppSettings["netatmo.username"];
            _password = ConfigurationManager.AppSettings["netatmo.password"];

            _isValidConfiguration =
                !string.IsNullOrEmpty(_clientId) &&
                !string.IsNullOrEmpty(_clientSecret) &&
                !string.IsNullOrEmpty(_username) &&
                !string.IsNullOrEmpty(_password);

            _canCreateDevices = false;
        }

        public override IDevice CreateEmptyDevice()
        {
            throw new NotSupportedException();
        }

        protected override Task ExecuteInternal(IDevice device, IAction action, IDictionary<string, string> values)
        {
            throw new NotSupportedException();
        }

        public async Task ScanDeviceAndDataAsync()
        {
            if (!_isValidConfiguration)
            {
                _messageQueue.Publish(new NotifyUserMessage("Add netatmo configuration to config file."));
                return;
            }

            var token = await GetTokenAsync();

            while (true)
            {
                try
                {
                    if (token.Expiration - DateTime.UtcNow < TimeSpan.FromSeconds(60))
                    {
                        token = await RefreshTokenAsync(token);
                    }

                    await GetDeviceData(token);
                }
                catch (Exception e)
                {
                    _log.Error(e.Message, e);
                }

                await Task.Delay(TimeSpan.FromMinutes(1));
            }
        }

        private async Task GetDeviceData(TokenResponseDto token)
        {
            var client = new RestClient("https://api.netatmo.com");
            var request = new RestRequest("/api/getstationsdata");
            request.AddQueryParameter("access_token", token.AccessToken);

            var data = await client.GetTaskAsync<StationDataResponseDto>(request);

            if (data?.Body?.Devices == null)
            {
                return;
            }

            lock (_deviceLock)
            {
                foreach (var dataDevice in data.Body.Devices)
                {
                    var station = dataDevice.StationName;
                    dataDevice.BatteryPercent = 100;

                    UpdateDevice(station, dataDevice);

                    foreach (var module in dataDevice.Modules)
                    {
                        UpdateDevice(station, module);
                    }
                }
            }
        }

        private void UpdateDevice(string station, IStationModule module)
        {
            var id = $"{station}-{module.ModuleName}";

            var device = _devices.SingleOrDefault(d => d.Id.Equals(id));

            if (device == null)
            {
                device = new NetatmoDevice(id, module.ModuleName);
                _devices.Add(device);
            }

            PublishVariables(device, module);

            if (module.BatteryPercent > 85)
            {
                device.BatteryStatus = DeviceBatteryStatus.Full;
            }
            else if (module.BatteryPercent > 25)
            {
                device.BatteryStatus = DeviceBatteryStatus.Good;
            }
            else
            {
                device.BatteryStatus = DeviceBatteryStatus.Low;
            }
        }

        private void PublishVariables(IDevice device, IStationModule module)
        {
            var properties = module.DashboardData.GetType().GetProperties();

            foreach (var property in properties)
            {
                if (module.DataType.Contains(property.Name))
                {
                    var name = property.Name[0] + property.Name.ToLowerInvariant().Substring(1);
                    var value = (double) property.GetValue(module.DashboardData);
                    _messageQueue.Publish(new UpdateVariableMessage(Name, device.Id, name, value));
                }
            }
        }

        private async Task<TokenResponseDto> GetTokenAsync()
        {
            var client = new RestClient("https://api.netatmo.com");
            var request = new RestRequest("/oauth2/token");
            request.AddHeader("Content-Type", "application/x-www-form-urlencoded;charset=UTF-8");
            request.AddParameter("grant_type", "password");
            request.AddParameter("client_id", _clientId);
            request.AddParameter("client_secret", _clientSecret);
            request.AddParameter("username", _username);
            request.AddParameter("password", _password);
            request.AddParameter("scope", "read_station read_thermostat");

            var token = await client.PostTaskAsync<TokenResponseDto>(request);
            token.Expiration = DateTime.UtcNow.AddSeconds(token.ExpiresIn);
            return token;
        }

        private async Task<TokenResponseDto> RefreshTokenAsync(TokenResponseDto dto)
        {
            var client = new RestClient("https://api.netatmo.com");
            var request = new RestRequest("/oauth2/token");
            request.AddHeader("Content-Type", "application/x-www-form-urlencoded;charset=UTF-8");
            request.AddParameter("grant_type", dto.RefreshToken);
            request.AddParameter("client_id", _clientId);
            request.AddParameter("client_secret", _clientSecret);

            var token = await client.PostTaskAsync<TokenResponseDto>(request);
            token.Expiration = DateTime.UtcNow.AddSeconds(token.ExpiresIn);
            return token;
        }

        public class StationModuleDto : IStationModule
        {
            public string Type { get; set; }
            public StationDashboardData DashboardData { get; set; }
            public List<string> DataType { get; set; }
            public string ModuleName { get; set; }
            public int BatteryPercent { get; set; }
        }

        public class StationDataResponseDto
        {
            public StationDataBody Body { get; set; }
        }

        public class StationDataBody
        {
            public List<StationDataDevice> Devices { get; set; }
        }

        public class StationDataDevice : IStationModule
        {
            public string StationName { get; set; }
            public string ModuleName { get; set; }
            public StationDashboardData DashboardData { get; set; }
            public List<StationModuleDto> Modules { get; set; }
            public List<string> DataType { get; set; }
            public int BatteryPercent { get; set; }
        }

        public interface IStationModule
        {
            string ModuleName { get; set; }
            StationDashboardData DashboardData { get; set; }
            List<string> DataType { get; set; }
            int BatteryPercent { get; set; }
        }

        public class StationDashboardData
        {
            public double AbsolutePressure { get; set; }
            public double Noise { get; set; }
            public double Temperature { get; set; }
            public double Humidity { get; set; }
            public double Pressure { get; set; }
            public double CO2 { get; set; }
        }

        public class TokenResponseDto
        {
            public string AccessToken { get; set; }
            public int ExpiresIn { get; set; }
            public string RefreshToken { get; set; }
            public DateTime Expiration { get; set; }
        }
    }
}