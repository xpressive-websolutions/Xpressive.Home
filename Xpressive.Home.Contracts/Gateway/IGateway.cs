using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Xpressive.Home.Contracts.Gateway
{
    public interface IGateway : IHostedService, IDisposable
    {
        string Name { get; }
        bool CanCreateDevices { get; }

        IEnumerable<IDevice> Devices { get; }
        IDevice CreateEmptyDevice();
        Task<bool> AddDevice(IDevice device);
        Task RemoveDevice(IDevice device);

        IEnumerable<IAction> GetActions(IDevice device);
    }
}
