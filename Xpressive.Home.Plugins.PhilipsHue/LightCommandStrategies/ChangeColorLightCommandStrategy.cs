using System;
using System.Collections.Generic;
using Q42.HueApi;
using Q42.HueApi.ColorConverters;
using Q42.HueApi.ColorConverters.HSB;

namespace Xpressive.Home.Plugins.PhilipsHue.LightCommandStrategies
{
    internal sealed class ChangeColorLightCommandStrategy : LightCommandStrategyBase
    {
        public override LightCommand GetLightCommand(IDictionary<string, string> values, PhilipsHueDevice bulb)
        {
            string hexColor;
            if (!values.TryGetValue("Color", out hexColor))
            {
                throw new ArgumentException("Color");
            }

            // TODO: http://www.developers.meethue.com/documentation/color-conversions-rgb-xy

            var command = new LightCommand();
            command.SetColor(new RGBColor(hexColor));

            if (!bulb.IsOn)
            {
                command.On = true;
            }

            TimeSpan transitionTime;
            if (TryGetTransitionTime(values, out transitionTime))
            {
                command.TransitionTime = transitionTime;
            }

            return command;
        }
    }
}
