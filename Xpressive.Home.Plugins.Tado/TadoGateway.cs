using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using RestSharp;
using Serilog;
using Xpressive.Home.Contracts.Gateway;
using Xpressive.Home.Contracts.Messaging;
using Action = Xpressive.Home.Contracts.Gateway.Action;

namespace Xpressive.Home.Plugins.Tado
{
    internal class TadoGateway : GatewayBase
    {
        private readonly object _deviceListLock = new object();
        private readonly RestClient _client;
        private readonly RestClient _authClient;
        private readonly string _username;
        private readonly string _password;
        private TokenDto _token;

        public TadoGateway(IMessageQueue messageQueue, IConfiguration configuration)
            : base(messageQueue, "tado", false)
        {
            _client = new RestClient("https://my.tado.com/");
            _authClient = new RestClient("https://auth.tado.com/");
            _username = configuration["tado.username"];
            _password = configuration["tado.password"];
        }

        public override IEnumerable<IAction> GetActions(IDevice device)
        {
            yield return new Action("Set temperature") { Fields = { "Temperature" } };
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ContinueWith(_ => { });

            if (string.IsNullOrEmpty(_username) || string.IsNullOrEmpty(_password))
            {
                MessageQueue.Publish(new NotifyUserMessage("Add tado configuration to config file."));
                return;
            }

            MeDto me;

            try
            {
                _token = await LoginAsync();
                me = await GetAsync<MeDto>("api/v1/me", _token);
            }
            catch (Exception e)
            {
                Log.Error(e.Message, e);
                return;
            }

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    _token = await RefreshTokenAsync(_token);

                    var zones = await GetAsync<List<ZoneDto>>($"api/v2/homes/{me.HomeId}/zones", _token);

                    foreach (var zone in zones)
                    {
                        var device = new TadoDevice(me.HomeId, zone.Id);

                        lock (_deviceListLock)
                        {
                            if (DeviceDictionary.TryGetValue(device.Id, out var d))
                            {
                                device = d as TadoDevice;
                            }
                            else
                            {
                                device = null;
                            }

                            if (device == null)
                            {
                                device = new TadoDevice(me.HomeId, zone.Id)
                                {
                                    Name = zone.Name,
                                    Icon = "fa fa-thermometer-full"
                                };

                                DeviceDictionary.TryAdd(device.Id, device);
                            }
                        }

                        var state = await GetAsync<StateDto>($"api/v2/homes/{me.HomeId}/zones/{zone.Id}/state", _token);

                        PublishIfNotNull(device.Id, "Mode", state?.TadoMode.ToString());
                        PublishIfNotNull(device.Id, "Temperature", state?.SensorDataPoints?.InsideTemperature?.Celsius, "°C");
                        PublishIfNotNull(device.Id, "Humidity", state?.SensorDataPoints?.Humidity?.Percentage, "%");
                        PublishIfNotNull(device.Id, "TargetTemperature", state?.Setting?.Temperature?.Celsius, "°C");
                        PublishIfNotNull(device.Id, "Power", state?.Setting?.Power.ToString());
                        PublishIfNotNull(device.Id, "Type", state?.Setting?.Type);
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e.Message, e);
                }

                await Task.Delay(TimeSpan.FromSeconds(60), cancellationToken).ContinueWith(_ => { });
            }
        }

        private void PublishIfNotNull(string deviceId, string variableName, string value, string unit = null)
        {
            if (value != null)
            {
                MessageQueue.Publish(new UpdateVariableMessage(Name, deviceId, variableName, value, unit));
            }
        }

        private void PublishIfNotNull(string deviceId, string variableName, double? value, string unit = null)
        {
            if (value.HasValue)
            {
                MessageQueue.Publish(new UpdateVariableMessage(Name, deviceId, variableName, Round(value.Value), unit));
            }
        }

        private static double Round(double value)
        {
            return Math.Round(value);
        }

        private async Task<T> GetAsync<T>(string url, TokenDto token) where T : new()
        {
            var request = new RestRequest(url);
            request.AddHeader("Authorization", "Bearer " + token.AccessToken);
            request.AddHeader("Referer", "https://my.tado.com/");
            return await _client.GetAsync<T>(request);
        }

        private async Task<TokenDto> LoginAsync()
        {
            //var httpClient = _httpClientProvider.Get();
            //var env = await httpClient.GetStringAsync("https://my.tado.com/webapp/env.js");
            //var clientSecret = env
            //    .Split(new[] {'\n', '\r'}, StringSplitOptions.RemoveEmptyEntries)
            //    .Select(l => l.Trim(' ', '\t'))
            //    .Where(l => l.StartsWith("clientSecret", StringComparison.OrdinalIgnoreCase))
            //    .Select(l => l.Substring(12).Trim(':', ' ', '\''))
            //    .SingleOrDefault();

            var loginRequest = CreateRestRequest("oauth/token", "password");
            loginRequest.AddQueryParameter("password", _password);
            loginRequest.AddQueryParameter("username", _username);
            loginRequest.AddQueryParameter("client_secret", "wZaRN7rpjn3FoNyF5IFuxg9uMzYJcvOoQ8QWiIqS3hfk6gLhVlG57j5YNoZL2Rtc");
            //loginRequest.AddHeader(":authority", "auth.tado.com");
            //loginRequest.AddHeader(":method", "POST");
            //loginRequest.AddHeader(":path", "/oauth/token");
            //loginRequest.AddHeader(":scheme", "https");
            loginRequest.AddHeader("Content-Type", "application/x-www-form-urlencoded");

            var token = await _authClient.PostAsync<TokenDto>(loginRequest);
            token.Expires = DateTime.UtcNow.AddSeconds(token.ExpiresIn);
            return token;
        }

        private async Task<TokenDto> RefreshTokenAsync(TokenDto token)
        {
            if ((token.Expires - DateTime.UtcNow).TotalMinutes > 1.5)
            {
                return token;
            }

            var refreshRequest = CreateRestRequest("oauth/token", "refresh_token");
            refreshRequest.AddQueryParameter("client_secret", "wZaRN7rpjn3FoNyF5IFuxg9uMzYJcvOoQ8QWiIqS3hfk6gLhVlG57j5YNoZL2Rtc");
            refreshRequest.AddQueryParameter("refresh_token", token.RefreshToken);

            token = await _authClient.PostAsync<TokenDto>(refreshRequest);
            token.Expires = DateTime.UtcNow.AddSeconds(token.ExpiresIn);
            return token;
        }

        private RestRequest CreateRestRequest(string resource, string grantType)
        {
            var restRequest = new RestRequest(resource);
            restRequest.AddQueryParameter("client_id", "tado-web-app");
            restRequest.AddQueryParameter("grant_type", grantType);
            restRequest.AddQueryParameter("scope", "home.user");
            restRequest.AddHeader("origin", "https://my.tado.com");
            restRequest.AddHeader("referer", "https://my.tado.com/");
            return restRequest;
        }

        public override IDevice CreateEmptyDevice()
        {
            throw new NotSupportedException();
        }

        protected override async Task ExecuteInternalAsync(IDevice device, IAction action, IDictionary<string, string> values)
        {
            if (device == null)
            {
                Log.Warning("Unable to execute action {actionName} because the device was not found.", action.Name);
                return;
            }

            string temp;
            double temperature;

            if (!values.TryGetValue("Temperature", out temp) || !double.TryParse(temp, out temperature))
            {
                return;
            }

            var d = device as TadoDevice;
            if (d == null)
            {
                return;
            }

            if (string.Equals(action.Name, "Set temperature", StringComparison.OrdinalIgnoreCase))
            {
                var payload = new
                {
                    setting = new
                    {
                        type = "HEATING",
                        power = "ON",
                        temperature = new
                        {
                            celsius = temperature.ToString("F0")
                        }
                    },
                    termination = new
                    {
                        type = "MANUAL"
                    }
                };

                var url = $"/api/v2/homes/{d.HomeId}/zones/{d.ZoneId}/overlay";
                var request = new RestRequest(url);
                request.AddHeader("Authorization", "Bearer " + _token.AccessToken);
                request.AddHeader("Content-Type", "application/json;charset=UTF-8");
                request.AddJsonBody(payload);

                var response = await _client.PutAsync<object>(request);
                response.ToString();
            }
        }
    }
}
