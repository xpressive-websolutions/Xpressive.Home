using System;
using System.Threading;

namespace Xpressive.Home.Plugins.PhilipsHue
{
    internal interface IPhilipsHueDeviceDiscoveringService
    {
        void Start(CancellationToken cancellationToken);

        event EventHandler<PhilipsHueBulb> BulbFound;

        event EventHandler<PhilipsHuePresenceSensor> PresenceSensorFound;

        event EventHandler<PhilipsHueButtonSensor> ButtonSensorFound;
    }
}
