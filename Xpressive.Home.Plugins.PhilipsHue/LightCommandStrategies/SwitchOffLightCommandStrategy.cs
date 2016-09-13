using System;
using System.Collections.Generic;
using Q42.HueApi;

namespace Xpressive.Home.Plugins.PhilipsHue.LightCommandStrategies
{
    internal sealed class SwitchOffLightCommandStrategy : LightCommandStrategyBase
    {
        public override LightCommand GetLightCommand(IDictionary<string, string> values, PhilipsHueDevice bulb)
        {
            var command = new LightCommand();

            if (bulb.IsOn)
            {
                command.On = false;

                TimeSpan transitionTime;
                if (TryGetTransitionTime(values, out transitionTime))
                {
                    command.TransitionTime = transitionTime;
                }
            }

            return command;
        }
    }
}
