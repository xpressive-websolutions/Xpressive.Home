using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xpressive.Home.Contracts.Services;

namespace Xpressive.Home.Services
{
    internal sealed class NetworkDeviceService : INetworkDeviceService
    {
        private readonly IList<INetworkDeviceScanner> _scanners;

        public NetworkDeviceService(IEnumerable<INetworkDeviceScanner> scanners)
        {
            _scanners = scanners.ToList();
        }

        public async Task<IList<NetworkDevice>> GetAvailableNetworkDevicesAsync(CancellationToken cancellationToken)
        {
            var tasks = _scanners
                .Select(s => s.GetAvailableNetworkDevicesAsync(cancellationToken))
                .ToList();

            await Task.WhenAll(tasks);

            var result = tasks
                .Where(t => !t.IsFaulted && !t.IsCanceled)
                .SelectMany(t => t.Result)
                .Distinct()
                .ToList();

            return result;
        }
    }
}
