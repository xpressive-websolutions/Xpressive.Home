using System;

namespace Xpressive.Home.Plugins.PhilipsHue
{
    internal interface IPhilipsHueBridgeDiscoveringService
    {
        event EventHandler<PhilipsHueBridge> BridgeFound;
    }
}