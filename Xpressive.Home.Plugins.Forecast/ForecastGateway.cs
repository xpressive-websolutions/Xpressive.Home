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
        private readonly AutoResetEvent _taskWaitHandle = new AutoResetEvent(false);
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
                _taskWaitHandle.Set();
                return;
            }

            while (_isRunning)
            {
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
                await TaskHelper.DelayAsync(TimeSpan.FromMinutes(minutes), () => _isRunning);
            }

            _taskWaitHandle.Set();
        }

        public override void Stop()
        {
            _isRunning = false;
            if (!_taskWaitHandle.WaitOne(TimeSpan.FromSeconds(5)))
            {
                _log.Error("Unable to shutdown.");
            }
        }

        protected override Task ExecuteInternalAsync(IDevice device, IAction action, IDictionary<string, string> values)
        {
            throw new NotSupportedException();
        }

        protected override void Dispose(bool disposing)
        {
            _isRunning = false;
            if (disposing)
            {
                _taskWaitHandle.Dispose();
            }
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

            UpdateVariables(device.Id, string.Empty, response.currently);

            for (var hour = 0; hour < response.hourly.data.Count; hour++)
            {
                var data = response.hourly.data[hour];
                UpdateVariables(device.Id, $"H+{hour}_", data);
            }

            for (var day = 0; day < response.daily.data.Count; day++)
            {
                var data = response.daily.data[day];
                UpdateVariables(device.Id, $"D+{day}_", data);
            }

            await TaskHelper.DelayAsync(TimeSpan.FromSeconds(10), () => _isRunning);
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
