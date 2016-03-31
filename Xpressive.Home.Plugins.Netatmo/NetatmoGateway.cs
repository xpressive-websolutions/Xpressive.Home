using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using RestSharp;
using Xpressive.Home.Contracts.Gateway;
using Xpressive.Home.Contracts.Messaging;

namespace Xpressive.Home.Plugins.Netatmo
{
    internal class NetatmoGateway : GatewayBase
    {
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
                if (token.Expiration - DateTime.UtcNow < TimeSpan.FromSeconds(60))
                {
                    token = await RefreshTokenAsync(token);
                }

                await GetDeviceData(token);

                await Task.Delay(TimeSpan.FromMinutes(1));
            }
        }

        private async Task GetDeviceData(TokenResponseDto token)
        {
            var client = new RestClient("https://api.netatmo.com");
            var request = new RestRequest("/api/getstationsdata");
            request.AddQueryParameter("access_token", token.AccessToken);

            var data = await client.GetTaskAsync<StationDataResponseDto>(request);

            lock (_deviceLock)
            {
                foreach (var dataDevice in data.Body.Devices)
                {
                    var deviceName = dataDevice.StationName;
                    var device = _devices.SingleOrDefault(d => d.Id.Equals(deviceName));

                    if (device == null)
                    {
                        device = new NetatmoDevice(deviceName, deviceName);
                        _devices.Add(device);
                    }

                    var module = dataDevice.ModuleName;
                    PublishVariables(device, module, dataDevice.DashboardData, dataDevice.DataType);

                    foreach (var moduleDto in dataDevice.Modules)
                    {
                        PublishVariables(device, moduleDto.ModuleName, moduleDto.DashboardData, moduleDto.DataType);
                    }
                }
            }
        }

        private void PublishVariables(IDevice device, string module, StationDashboardData dashboardData, List<string> dataTypes)
        {
            var properties = dashboardData.GetType().GetProperties();

            foreach (var property in properties)
            {
                if (dataTypes.Contains(property.Name))
                {
                    var name = property.Name.ToLowerInvariant();
                    var value = (double) property.GetValue(dashboardData);
                    _messageQueue.Publish(new UpdateVariableMessage(Name, device.Id, $"{module}_{name}", value));
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

        public class StationModuleDto
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

        public class StationDataDevice
        {
            public string StationName { get; set; }
            public string ModuleName { get; set; }
            public StationDashboardData DashboardData { get; set; }
            public List<StationModuleDto> Modules { get; set; }
            public List<string> DataType { get; set; }
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