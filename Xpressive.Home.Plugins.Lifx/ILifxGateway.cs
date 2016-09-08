using System.Collections.Generic;
using Xpressive.Home.Contracts.Gateway;

namespace Xpressive.Home.Plugins.Lifx
{
    internal interface ILifxGateway : IGateway
    {
        IEnumerable<LifxDevice> GetDevices();

        void SwitchOn(LifxDevice device, int transitionTimeInSeconds);
        void SwitchOff(LifxDevice device, int transitionTimeInSeconds);
        void ChangeColor(LifxDevice device, string hexColor, int transitionTimeInSeconds);
        void ChangeBrightness(LifxDevice device, double brightness, int transitionTimeInSeconds);
    }
}
