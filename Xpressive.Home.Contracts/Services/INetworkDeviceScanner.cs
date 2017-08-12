using System.Threading;
using System.Threading.Tasks;

namespace Xpressive.Home.Contracts.Services
{
    /// <summary>
    /// Interface to be implemented by network scanners.
    /// </summary>
    public interface INetworkDeviceScanner
    {
        Task StartAsync(CancellationToken token);
    }
}
