using System;
using System.Collections.Generic;
using Q42.HueApi;

namespace Xpressive.Home.Plugins.PhilipsHue.LightCommandStrategies
{
    internal sealed class SwitchOnLightCommandStrategy : LightCommandStrategyBase
    {
        public override LightCommand GetLightCommand(IDictionary<string, string> values, PhilipsHueDevice bulb)
        {
            var command = new LightCommand
            {
                On = true
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
