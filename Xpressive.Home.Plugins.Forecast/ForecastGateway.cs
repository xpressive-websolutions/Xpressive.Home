using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using ForecastIO;
using log4net;
using Xpressive.Home.Contracts.Gateway;
using Xpressive.Home.Contracts.Messaging;

namespace Xpressive.Home.Plugins.Forecast
{
    public class ForecastGateway : GatewayBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(ForecastGateway));
        private readonly string _apiKey;
        private readonly IMessageQueue _messageQueue;

        public ForecastGateway(IMessageQueue messageQueue) : base("Weather")
        {
            _messageQueue = messageQueue;
            _apiKey = ConfigurationManager.AppSettings["forecast.apikey"];
            _canCreateDevices = true;
        }

        public override IDevice CreateEmptyDevice()
        {
            return new ForecastDevice();
        }

        public async Task StartAsync()
        {
            await LoadDevicesAsync((id, name) => new ForecastDevice { Id = id, Name = name });

            while (true)
            {
                var recentUpdate = DateTime.UtcNow;

                if (string.IsNullOrEmpty(_apiKey))
                {
                    _log.Warn("ApiKey for forcast.io (forecast.apikey) not in config file.");
                }
                else
                {
                    var devices = _devices.Cast<ForecastDevice>().ToList();
                    devices.ForEach(async d => await GetWeatherInfo(d));
                }

                var minutes = Math.Max(_devices.Count * 2.5, 10);
                var nextUpdate = recentUpdate + TimeSpan.FromMinutes(minutes);
                await Task.Delay(nextUpdate - recentUpdate);
            }
        }

        protected override Task ExecuteInternal(IDevice device, IAction action, IDictionary<string, string> values)
        {
            throw new NotSupportedException();
        }

        private async Task GetWeatherInfo(ForecastDevice device)
        {
            var latitude = (float)device.Latitude;
            var longitude = (float)device.Longitude;
            var request = new ForecastIORequest(_apiKey, latitude, longitude, Unit.si);
            var response = request.Get();

            UpdateVariables(device.Id, response.currently);

            for (var hour = 0; hour < response.hourly.data.Count; hour++)
            {
                var deviceId = $"{device.Id}_H+{hour}";
                var data = response.hourly.data[hour];
                UpdateVariables(deviceId, data);
            }

            for (var day = 0; day < response.daily.data.Count; day++)
            {
                var deviceId = $"{device.Id}_D+{day}";
                var data = response.daily.data[day];
                UpdateVariables(deviceId, data);
            }

            await Task.Delay(TimeSpan.FromSeconds(10));
        }

        private void UpdateVariables(string deviceId, object data)
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

            UpdateVariables(deviceId, dict, doubleParameters, v => Math.Round((float)v, 2));
            UpdateVariables(deviceId, dict, stringParameters, v => v);
        }

        private void UpdateVariables(string deviceId, IDictionary<string, object> values, IEnumerable<string> properties, Func<object, object> convert)
        {
            foreach (var p in properties)
            {
                object v;
                if (!values.TryGetValue(p, out v))
                {
                    continue;
                }
                var name = CultureInfo.InvariantCulture.TextInfo.ToUpper(p[0]) + p.Substring(1);
                var converted = convert(v);
                _messageQueue.Publish(new UpdateVariableMessage(Name, deviceId, name, converted));
            }
        }
    }
}
