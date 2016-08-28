using System;

namespace Xpressive.Home.Contracts.Services
{
    public interface IUpnpDeviceDiscoveringService : IDisposable
    {
        event EventHandler<IUpnpDeviceResponse> DeviceFound;
    }
}
