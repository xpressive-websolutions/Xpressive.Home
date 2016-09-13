using System;
using System.Collections.Generic;
using Q42.HueApi;

namespace Xpressive.Home.Plugins.PhilipsHue.LightCommandStrategies
{
    internal sealed class ChangeTemperatureLightCommandStrategy : LightCommandStrategyBase
    {
        public override LightCommand GetLightCommand(IDictionary<string, string> values, PhilipsHueDevice bulb)
        {
            int temperature;
            if (!TryGetTemperature(values, out temperature))
            {
                throw new ArgumentException("Temperature");
            }

            return new LightCommand
            {
                ColorTemperature = temperature
            };
        }

        private bool TryGetTemperature(IDictionary<string, string> values, out int temperature)
        {
            temperature = 0;
            string st;
            int it;

            if (values.TryGetValue("Temperature", out st) && int.TryParse(st, out it) && it >= 2000 && it <= 6500)
            {
                temperature = KelvinToMirek(it);
                return true;
            }

            return false;
        }

        private int KelvinToMirek(int kelvin)
        {
            return 1000000 / kelvin;
        }
    }
}
