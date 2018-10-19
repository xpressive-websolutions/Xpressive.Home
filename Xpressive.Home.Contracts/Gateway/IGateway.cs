using System;
using System.Collections.Generic;
using Microsoft.Extensions.Hosting;

namespace Xpressive.Home.Contracts.Gateway
{
    public interface IGateway : IHostedService, IDisposable
    {
        string Name { get; }
        bool CanCreateDevices { get; }

        IEnumerable<IDevice> Devices { get; }
        IDevice CreateEmptyDevice();
        bool AddDevice(IDevice device);
        void RemoveDevice(IDevice device);

        IEnumerable<IAction> GetActions(IDevice device);
    }
}
