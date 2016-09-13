using System;
using System.Collections.Generic;
using Q42.HueApi;
using Xpressive.Home.Contracts.Gateway;

namespace Xpressive.Home.Plugins.PhilipsHue.LightCommandStrategies
{
    internal abstract class LightCommandStrategyBase
    {
        private static readonly IDictionary<string, LightCommandStrategyBase> _strategies =
            new Dictionary<string, LightCommandStrategyBase>(StringComparer.OrdinalIgnoreCase)
            {
                {"Change Brightness", new ChangeBrightnessLightCommandStrategy()},
                {"Change Color", new ChangeColorLightCommandStrategy()},
                {"Switch On", new SwitchOnLightCommandStrategy()},
                {"Switch Off", new SwitchOffLightCommandStrategy()},
                {"Change Temperature", new ChangeTemperatureLightCommandStrategy()},
                {"Alarm Multiple", new AlarmMultipleLightCommandStrategy()},
                {"Alarm Once", new AlarmOnceLightCommandStrategy()}
            };

        public static LightCommandStrategyBase Get(IAction action)
        {
            LightCommandStrategyBase strategy;
            if (_strategies.TryGetValue(action.Name, out strategy))
            {
                return strategy;
            }
            throw new NotSupportedException(action.Name);
        }

        public abstract LightCommand GetLightCommand(IDictionary<string, string> values, PhilipsHueDevice bulb);

        protected bool TryGetTransitionTime(IDictionary<string, string> values, out TimeSpan transitionTime)
        {
            string seconds;
            int s;
            transitionTime = default(TimeSpan);

            if (values.TryGetValue("Transition time in seconds", out seconds) && int.TryParse(seconds, out s) && s > 0)
            {
                transitionTime = TimeSpan.FromSeconds(s);
                return true;
            }

            return false;
        }
    }
}
