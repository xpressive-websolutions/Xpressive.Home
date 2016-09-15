using System;
using System.Collections.Generic;
using Q42.HueApi;

namespace Xpressive.Home.Plugins.PhilipsHue.LightCommandStrategies
{
    internal sealed class ChangeBrightnessLightCommandStrategy : LightCommandStrategyBase
    {
        public override LightCommand GetLightCommand(IDictionary<string, string> values, PhilipsHueDevice bulb)
        {
            byte brightness;
            if (!TryGetBrightness(values, out brightness))
            {
                throw new ArgumentException("Brightness");
            }

            var command = new LightCommand
            {
                Brightness = brightness,
                On = true
            };

            TimeSpan transitionTime;
            if (TryGetTransitionTime(values, out transitionTime))
            {
                command.TransitionTime = transitionTime;
            }

            return command;
        }

        private bool TryGetBrightness(IDictionary<string, string> values, out byte brightness)
        {
            string sb;
            double db;
            brightness = 0;

            if (!values.TryGetValue("Brightness", out sb) || !double.TryParse(sb, out db))
            {
                return false;
            }
            if (db < 0d || db > 1d)
            {
                return false;
            }

            brightness = Convert.ToByte(db * 255);
            return true;
        }
    }
}
