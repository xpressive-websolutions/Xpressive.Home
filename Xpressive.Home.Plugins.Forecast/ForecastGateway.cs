using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ForecastIO;
using log4net;
using Xpressive.Home.Contracts;
using Xpressive.Home.Contracts.Gateway;
using Xpressive.Home.Contracts.Messaging;

namespace Xpressive.Home.Plugins.Forecast
{
    public class ForecastGateway : GatewayBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(ForecastGateway));
        private readonly string _apiKey;
        private readonly IMessageQueue _messageQueue;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(0);
        private bool _isRunning = true;

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

        public override async Task StartAsync()
        {
            await Task.Delay(TimeSpan.FromSeconds(1));

            await LoadDevicesAsync((id, name) => new ForecastDevice { Id = id, Name = name });

            if (string.IsNullOrEmpty(_apiKey))
            {
                _messageQueue.Publish(new NotifyUserMessage("Add forecast.io configuration to config file."));
                _semaphore.Release();
                return;
            }

            while (_isRunning)
            {
                var recentUpdate = DateTime.UtcNow;

                try
                {
                    var devices = _devices.Cast<ForecastDevice>().ToList();
                    devices.ForEach(async d => await GetWeatherInfo(d));
                }
                catch (Exception e)
                {
                    _log.Error(e.Message, e);
                }

                var minutes = Math.Max(_devices.Count*2.5, 10);
                var nextUpdate = recentUpdate + TimeSpan.FromMinutes(minutes);

                await TaskHelper.DelayAsync(nextUpdate - DateTime.UtcNow, () => _isRunning);
            }

            _semaphore.Release();
        }

        public override void Stop()
        {
            _isRunning = false;
            _semaphore.Wait(TimeSpan.FromSeconds(5));
        }

        protected override Task ExecuteInternal(IDevice device, IAction action, IDictionary<string, string> values)
        {
            throw new NotSupportedException();
        }

        protected override void Dispose(bool disposing)
        {
            _isRunning = false;
            base.Dispose(disposing);
        }

        private async Task GetWeatherInfo(ForecastDevice device)
        {
            var latitude = (float)device.Latitude;
            var longitude = (float)device.Longitude;
            ForecastIOResponse response;

            try
            {
                var request = new ForecastIORequest(_apiKey, latitude, longitude, Unit.si);
                response = request.Get();
            }
            catch (Exception e)
            {
                _log.Error(e.Message, e);
                return;
            }

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

            await TaskHelper.DelayAsync(TimeSpan.FromSeconds(10), () => _isRunning);
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
