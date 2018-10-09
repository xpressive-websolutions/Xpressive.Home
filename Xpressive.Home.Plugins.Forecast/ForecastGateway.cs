using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using DarkSky.Services;
using Microsoft.Extensions.Configuration;
using Polly;
using Serilog;
using Xpressive.Home.Contracts.Gateway;
using Xpressive.Home.Contracts.Messaging;

namespace Xpressive.Home.Plugins.Forecast
{
    public class ForecastGateway : GatewayBase
    {
        private readonly string _apiKey;
        private readonly IMessageQueue _messageQueue;
        private readonly Policy _policy;
        private DarkSkyService _darkSky;

        public ForecastGateway(IMessageQueue messageQueue, IConfiguration configuration) : base("Weather")
        {
            _messageQueue = messageQueue;
            _apiKey = configuration["forecast.apikey"];
            _canCreateDevices = true;

            _policy = Policy
                .Handle<WebException>()
                .WaitAndRetry(new[]
                {
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(2),
                    TimeSpan.FromSeconds(5)
                });
        }

        public override IDevice CreateEmptyDevice()
        {
            return new ForecastDevice();
        }

        public override IEnumerable<IAction> GetActions(IDevice device)
        {
            yield break;
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ContinueWith(_ => { });

            await LoadDevicesAsync((id, name) => new ForecastDevice { Id = id, Name = name });

            if (string.IsNullOrEmpty(_apiKey))
            {
                _messageQueue.Publish(new NotifyUserMessage("Add forecast.io configuration to config file."));
                return;
            }

            _darkSky = new DarkSkyService(_apiKey);

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var devices = _devices.Cast<ForecastDevice>().ToList();
                    devices.ForEach(async d => await GetWeatherInfo(d, cancellationToken));
                }
                catch (Exception e)
                {
                    Log.Error(e, e.Message);
                }

                var minutes = Math.Max(_devices.Count * 2.5, 10);
                await Task.Delay(TimeSpan.FromMinutes(minutes), cancellationToken).ContinueWith(_ => { });
            }
        }

        protected override Task ExecuteInternalAsync(IDevice device, IAction action, IDictionary<string, string> values)
        {
            throw new NotSupportedException();
        }

        private async Task GetWeatherInfo(ForecastDevice device, CancellationToken cancellationToken)
        {
            var latitude = (float)device.Latitude;
            var longitude = (float)device.Longitude;
            DarkSky.Models.Forecast response;

            try
            {
                response = await _policy.ExecuteAsync(async () =>
                {
                    var r = await _darkSky.GetForecast(latitude, longitude, new DarkSkyService.OptionalParameters
                    {
                        MeasurementUnits = "si"
                    });

                    if (!r.IsSuccessStatus)
                    {
                        throw new WebException(r.ResponseReasonPhrase);
                    }

                    return r.Response;
                });
            }
            catch (WebException)
            {
                return;
            }
            catch (Exception e)
            {
                Log.Error(e, e.Message);
                return;
            }

            UpdateVariables(device.Id, string.Empty, response.Currently);

            for (var hour = 0; hour < response.Hourly.Data.Count; hour++)
            {
                var data = response.Hourly.Data[hour];
                UpdateVariables(device.Id, $"H+{hour}_", data);
            }

            for (var day = 0; day < response.Daily.Data.Count; day++)
            {
                var data = response.Daily.Data[day];
                UpdateVariables(device.Id, $"D+{day}_", data);
            }

            await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken).ContinueWith(_ => { });
        }

        private void UpdateVariables(string deviceId, string prefix, object data)
        {
            var properties = data.GetType().GetProperties();
            var dict = properties.ToDictionary(p => p.Name, p => p.GetValue(data));

            var doubleParameters = new[] {
                "apparentTemperature",
                "apparentTemperatureMax",
                "apparentTemperatureMin",
                "cloudCover",
                "dewPoint",
                "humidity",
                "moonPhase",
                "ozone",
                "precipIntensity",
                "precipProbability",
                "pressure",
                "temperature",
                "temperatureMax",
                "temperatureMin",
                "windSpeed",
            };

            var stringParameters = new[] { "icon", "summary" };

            UpdateVariables(deviceId, prefix, dict, doubleParameters, v => Math.Round((float)v, 2));
            UpdateVariables(deviceId, prefix, dict, stringParameters, v => v);
        }

        private void UpdateVariables(string deviceId, string prefix, IDictionary<string, object> values, IEnumerable<string> properties, Func<object, object> convert)
        {
            foreach (var p in properties)
            {
                object v;
                if (!values.TryGetValue(p, out v))
                {
                    continue;
                }
                var name = prefix + CultureInfo.InvariantCulture.TextInfo.ToUpper(p[0]) + p.Substring(1);
                var converted = convert(v);
                _messageQueue.Publish(new UpdateVariableMessage(Name, deviceId, name, converted));
            }
        }
    }
}
