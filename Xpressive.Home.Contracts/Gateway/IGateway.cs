using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Xpressive.Home.Contracts.Gateway
{
    public interface IGateway : IDisposable
    {
        string Name { get; }
        bool CanCreateDevices { get; }

        IEnumerable<IDevice> Devices { get; }
        IDevice CreateEmptyDevice();
        bool AddDevice(IDevice device);
        void RemoveDevice(IDevice device);

        IEnumerable<IAction> GetActions(IDevice device);

        Task StartAsync(CancellationToken cancellationToken);
    }
}
