using System;

namespace Xpressive.Home.Plugins.PhilipsHue
{
    internal interface IPhilipsHueDeviceDiscoveringService
    {
        void Start();

        event EventHandler<PhilipsHueBulb> BulbFound;

        event EventHandler<PhilipsHuePresenceSensor> PresenceSensorFound;
    }
}
