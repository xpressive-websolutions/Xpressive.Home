using System.Collections.Generic;
using Q42.HueApi;

namespace Xpressive.Home.Plugins.PhilipsHue.LightCommandStrategies
{
    internal sealed class AlarmMultipleLightCommandStrategy : LightCommandStrategyBase
    {
        public override LightCommand GetLightCommand(IDictionary<string, string> values, PhilipsHueDevice bulb)
        {
            return new LightCommand
            {
                Alert = Alert.Multiple
            };
        }
    }
}
