using System;

namespace Xpressive.Home.Plugins.PhilipsHue
{
    internal interface IPhilipsHueDeviceDiscoveringService
    {
        event EventHandler<PhilipsHueBulb> BulbFound;

        event EventHandler<PhilipsHuePresenceSensor> PresenceSensorFound;
    }
}
