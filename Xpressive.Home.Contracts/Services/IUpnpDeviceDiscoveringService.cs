using System;

namespace Xpressive.Home.Contracts.Services
{
    public interface IUpnpDeviceDiscoveringService
    {
        event EventHandler<IUpnpDeviceResponse> DeviceFound;
    }
}
