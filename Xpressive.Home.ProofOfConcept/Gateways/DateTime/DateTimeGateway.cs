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

            _properties.Add("Date");
            _properties.Add("Day");
            _properties.Add("Time");
            _properties.Add("Hour");
            _properties.Add("Minute");
            _properties.Add("Second");

            Observe();
        }

        protected override Task<string> GetInternal(IDevice device, string property)
        {
            var now = System.DateTime.Now;

            switch (property.ToLowerInvariant())
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

        protected override Task ExecuteInternal(IDevice device, IAction action, IDictionary<string, string> values)
        {
            return Task.CompletedTask;
        }

        private async Task Observe()
        {
            var previousTime = System.DateTime.UtcNow;
            var device = _devices.Single();
            var timeProperty = Properties.Single(p => p.Equals("Time", StringComparison.Ordinal));
            var dateProperty = Properties.Single(p => p.Equals("Date", StringComparison.Ordinal));
            var dayProperty = Properties.Single(p => p.Equals("Day", StringComparison.Ordinal));
            var hourProperty = Properties.Single(p => p.Equals("Hour", StringComparison.Ordinal));
            var minuteProperty = Properties.Single(p => p.Equals("Minute", StringComparison.Ordinal));
            var secondProperty = Properties.Single(p => p.Equals("Second", StringComparison.Ordinal));

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