using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Xpressive.Home.Plugins.NissanLeaf
{
    internal interface INissanLeafClient
    {
        Task<bool> InitAsync();

        Task<List<NissanLeafDevice>> LoginAsync(string username, string password);

        Task<BatteryStatus> GetBatteryStatusAsync(NissanLeafDevice device, CancellationToken cancellationToken);

        Task ActivateClimateControl(NissanLeafDevice device);
        Task DeactivateClimateControl(NissanLeafDevice device);
        Task StartCharging(NissanLeafDevice device);
    }
}
