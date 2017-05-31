using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Xpressive.Home.Contracts.Services
{
    /// <summary>
    /// Interface to be implemented by network scanners. Don't use this interface in gateways,
    /// you can't be sure that there is an implementation.
    /// Use <see cref="INetworkDeviceService"/> instead.
    /// </summary>
    public interface INetworkDeviceScanner
    {
        Task<IList<NetworkDevice>> GetAvailableNetworkDevicesAsync(CancellationToken cancellationToken);
    }
}
