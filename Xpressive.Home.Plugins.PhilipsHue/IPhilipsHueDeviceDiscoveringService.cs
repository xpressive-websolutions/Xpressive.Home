using System;

namespace Xpressive.Home.Plugins.PhilipsHue
{
    internal interface IPhilipsHueDeviceDiscoveringService
    {
        event EventHandler<PhilipsHueDevice> BulbFound;
    }
}