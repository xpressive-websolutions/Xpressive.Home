using System;

namespace Xpressive.Home.Plugins.PhilipsHue
{
    internal interface IPhilipsHueBridgeDiscoveringService
    {
        void Start();

        event EventHandler<PhilipsHueBridge> BridgeFound;
    }
}
