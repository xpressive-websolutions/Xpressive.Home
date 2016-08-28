using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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

        Task StartAsync();
        void Stop();
    }
}
