using System;
using System.Collections.Generic;
using Q42.HueApi;
using Q42.HueApi.ColorConverters;

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
            var rgb = new RGBColor(hexColor);
            var xy = RgbToCieConverter.Convert(bulb.Model, rgb.R, rgb.G, rgb.B);

            if (Equals(xy, default(RgbToCieConverter.CieResult)))
            {
                return new LightCommand();
            }

            var command = new LightCommand
            {
                On = true,
                ColorCoordinates = new[] {xy.X, xy.Y}
            };

            TimeSpan transitionTime;
            if (TryGetTransitionTime(values, out transitionTime))
            {
                command.TransitionTime = transitionTime;
            }

            return command;
        }
    }
}
