using System.Collections.Generic;

namespace Xpressive.Home.Plugins.PhilipsHue
{
    internal interface IPhilipsHueGateway
    {
        IEnumerable<PhilipsHueDevice> GetDevices();

        void SwitchOn(PhilipsHueDevice device, int transitionTimeInSeconds);
        void SwitchOff(PhilipsHueDevice device, int transitionTimeInSeconds);
        void ChangeColor(PhilipsHueDevice device, string hexColor, int transitionTimeInSeconds);
        void ChangeBrightness(PhilipsHueDevice device, double brightness, int transitionTimeInSeconds);
        void ChangeTemperature(PhilipsHueDevice device, int temperature);
    }
}
