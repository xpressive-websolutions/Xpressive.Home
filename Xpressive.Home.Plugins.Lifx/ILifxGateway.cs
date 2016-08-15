using System.Collections.Generic;

namespace Xpressive.Home.Plugins.Lifx
{
    internal interface ILifxGateway
    {
        IEnumerable<LifxDevice> GetDevices();

        void SwitchOn(LifxDevice device, int transitionTimeInSeconds);
        void SwitchOff(LifxDevice device, int transitionTimeInSeconds);
        void ChangeColor(LifxDevice device, string hexColor, int transitionTimeInSeconds);
        void ChangeBrightness(LifxDevice device, double brightness, int transitionTimeInSeconds);
    }
}
