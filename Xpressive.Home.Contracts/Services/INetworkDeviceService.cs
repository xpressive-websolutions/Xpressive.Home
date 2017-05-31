using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Xpressive.Home.Contracts.Services
{
    /// <summary>
    /// Get all <see cref="INetworkDeviceScanner"/> instances and returns
    /// a distinct set of all available network devices.
    /// </summary>
    public interface INetworkDeviceService
    {
        Task<IList<NetworkDevice>> GetAvailableNetworkDevicesAsync(CancellationToken cancellationToken);
    }
}
