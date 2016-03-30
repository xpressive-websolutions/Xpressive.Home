using System;

namespace Xpressive.Home.Plugins.Sonos
{
    internal interface ISonosDeviceDiscoverer
    {
        event EventHandler<SonosDevice> DeviceFound;
    }
}