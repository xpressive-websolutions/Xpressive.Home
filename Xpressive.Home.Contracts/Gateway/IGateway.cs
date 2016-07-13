using System;
using System.Collections.Generic;

namespace Xpressive.Home.Contracts.Gateway
{
    public interface IGateway : IDisposable
    {
        string Name { get; }
        bool CanCreateDevices { get; }

        IEnumerable<IDevice> Devices { get; }
        IEnumerable<IAction> Actions { get; }

        IDevice CreateEmptyDevice();
        bool AddDevice(IDevice device);
    }
}