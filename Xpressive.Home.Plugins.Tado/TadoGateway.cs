using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using RestSharp;
using Xpressive.Home.Contracts.Gateway;
using Xpressive.Home.Contracts.Messaging;

namespace Xpressive.Home.Plugins.Tado
{
    internal class TadoGateway : GatewayBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(TadoGateway));
        private readonly object _deviceListLock = new object();
        private readonly IMessageQueue _messageQueue;
        private readonly RestClient _client;
        private readonly string _username;
        private readonly string _password;

        public TadoGateway(IMessageQueue messageQueue) : base("tado")
        {
            _messageQueue = messageQueue;

            _client = new RestClient("https://my.tado.com/");
            _username = ConfigurationManager.AppSettings["tado.username"];
            _password = ConfigurationManager.AppSettings["tado.password"];
        }

        public override IEnumerable<IAction> GetActions(IDevice device)
        {
            yield break;
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ContinueWith(_ => { });

            if (string.IsNullOrEmpty(_username) || string.IsNullOrEmpty(_password))
            {
                _messageQueue.Publish(new NotifyUserMessage("Add tado configuration to config file."));
                return;
            }

            TokenDto token;
            MeDto me;

            try
            {
                token = await LoginAsync();
                me = await GetAsync<MeDto>("api/v1/me", token);
            }
            catch (Exception e)
            {
                _log.Error(e.Message, e);
                return;
            }

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    token = await RefreshTokenAsync(token);

                    var zones = await GetAsync<List<ZoneDto>>($"api/v2/homes/{me.HomeId}/zones", token);

                    foreach (var zone in zones)
                    {
                        var deviceId = me.HomeId + "_" + zone.Id;

                        lock (_deviceListLock)
                        {
                            if (!_devices.Cast<TadoDevice>().Any(d => d.Id.Equals(deviceId, StringComparison.OrdinalIgnoreCase)))
                            {
                                _devices.Add(new TadoDevice { Id = deviceId, Name = zone.Name, Icon = "fa fa-thermometer-full" });
                            }
                        }

                        var state = await GetAsync<StateDto>($"api/v2/homes/{me.HomeId}/zones/{zone.Id}/state", token);

                        PublishIfNotNull(deviceId, "Mode", state?.TadoMode.ToString());
                        PublishIfNotNull(deviceId, "Temperature", state?.SensorDataPoints?.InsideTemperature?.Celsius, "°C");
                        PublishIfNotNull(deviceId, "Humidity", state?.SensorDataPoints?.Humidity?.Percentage, "%");
                        PublishIfNotNull(deviceId, "TargetTemperature", state?.Setting?.Temperature?.Celsius, "°C");
                        PublishIfNotNull(deviceId, "Power", state?.Setting?.Power.ToString());
                        PublishIfNotNull(deviceId, "Type", state?.Setting?.Type);
                    }
                }
                catch (Exception e)
                {
                    _log.Error(e.Message, e);
                }

                await Task.Delay(TimeSpan.FromSeconds(60), cancellationToken).ContinueWith(_ => { });
            }
        }

        private void PublishIfNotNull(string deviceId, string variableName, string value, string unit = null)
        {
            if (value != null)
            {
                _messageQueue.Publish(new UpdateVariableMessage(Name, deviceId, variableName, value, unit));
            }
        }

        private void PublishIfNotNull(string deviceId, string variableName, double? value, string unit = null)
        {
            if (value.HasValue)
            {
                _messageQueue.Publish(new UpdateVariableMessage(Name, deviceId, variableName, Round(value.Value), unit));
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
            return await _client.GetTaskAsync<T>(request);
        }

        private async Task<TokenDto> LoginAsync()
        {
            var loginRequest = new RestRequest("oauth/token");
            loginRequest.AddQueryParameter("client_id", "tado-webapp");
            loginRequest.AddQueryParameter("grant_type", "password");
            loginRequest.AddQueryParameter("password", _password);
            loginRequest.AddQueryParameter("scope", "home.user");
            loginRequest.AddQueryParameter("username", _username);
            loginRequest.AddHeader("Origin", "https://my.tado.com");
            loginRequest.AddHeader("Referer", "https://my.tado.com/");

            var token = await _client.PostTaskAsync<TokenDto>(loginRequest);
            token.Expires = DateTime.UtcNow.AddSeconds(token.ExpiresIn);
            return token;
        }

        private async Task<TokenDto> RefreshTokenAsync(TokenDto token)
        {
            if ((token.Expires - DateTime.UtcNow).TotalMinutes > 1.5)
            {
                return token;
            }

            var refreshRequest = new RestRequest("oauth/token");
            refreshRequest.AddQueryParameter("client_id", "tado-webapp");
            refreshRequest.AddQueryParameter("grant_type", "refresh_token");
            refreshRequest.AddQueryParameter("refresh_token", token.RefreshToken);
            refreshRequest.AddQueryParameter("scope", "home.user");
            refreshRequest.AddHeader("Origin", "https://my.tado.com");
            refreshRequest.AddHeader("Referer", "https://my.tado.com/");

            token = await _client.PostTaskAsync<TokenDto>(refreshRequest);
            token.Expires = DateTime.UtcNow.AddSeconds(token.ExpiresIn);
            return token;
        }

        public override IDevice CreateEmptyDevice()
        {
            throw new NotSupportedException();
        }

        protected override Task ExecuteInternalAsync(IDevice device, IAction action, IDictionary<string, string> values)
        {
            throw new NotImplementedException();
        }
    }
}
