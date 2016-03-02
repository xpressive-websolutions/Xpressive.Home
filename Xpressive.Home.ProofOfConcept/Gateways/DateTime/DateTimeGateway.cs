using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Xpressive.Home.ProofOfConcept.Gateways.DateTime
{
    internal class DateTimeGateway : GatewayBase
    {
        public DateTimeGateway() : base("DateTime")
        {
            _devices.Add(new DateTimeDevice());

            _properties.Add(new DateProperty("Date"));
            _properties.Add(new WeekdayProperty("Day"));
            _properties.Add(new TimeProperty("Time"));
            _properties.Add(new NumericProperty("Hour", 0, 23));
            _properties.Add(new NumericProperty("Minute", 0, 59));
            _properties.Add(new NumericProperty("Second", 0, 59));

            Observe();
        }

        protected override Task<string> GetInternal(DeviceBase device, PropertyBase property)
        {
            var now = System.DateTime.Now;

            switch (property.Name.ToLowerInvariant())
            {
                case "time":
                    return Task.FromResult(now.ToString("HH:mm:ss"));
                case "date":
                    return Task.FromResult(now.Date.ToString("yy-MM-dd"));
                case "day":
                    return Task.FromResult(((((int)now.DayOfWeek) + 6) % 7).ToString());
                case "second":
                    return Task.FromResult(now.Second.ToString());
                case "minute":
                    return Task.FromResult(now.Minute.ToString());
                case "hour":
                    return Task.FromResult(now.Hour.ToString());
            }

            return Task.FromResult<string>(null);
        }

        protected override Task SetInternal(DeviceBase device, PropertyBase property, string value)
        {
            return Task.CompletedTask;
        }

        protected override Task ExecuteInternal(DeviceBase device, IAction action, IDictionary<string, string> values)
        {
            return Task.CompletedTask;
        }

        private async Task Observe()
        {
            var previousTime = System.DateTime.UtcNow;
            var device = _devices.Single();
            var timeProperty = GetProperty("Time");
            var dateProperty = GetProperty("Date");
            var dayProperty = GetProperty("Day");
            var hourProperty = GetProperty("Hour");
            var minuteProperty = GetProperty("Minute");
            var secondProperty = GetProperty("Second");

            while (true)
            {
                await Task.Delay(100);

                var now = System.DateTime.Now;

                if (now.Second != previousTime.Second)
                {
                    OnDevicePropertyChanged(device, dayProperty, ((((int)now.DayOfWeek) + 6) % 7).ToString());
                    OnDevicePropertyChanged(device, dateProperty, now.ToString("yy-MM-dd"));
                    OnDevicePropertyChanged(device, hourProperty, now.Hour.ToString());
                    OnDevicePropertyChanged(device, minuteProperty, now.Minute.ToString());
                    OnDevicePropertyChanged(device, secondProperty, now.Second.ToString());
                    OnDevicePropertyChanged(device, timeProperty, now.ToString("HH:mm:ss"));

                    previousTime = now;
                }
            }
        }
    }
}