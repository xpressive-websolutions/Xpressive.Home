using System.Threading.Tasks;
using Xpressive.Home.Contracts.Gateway;

namespace Xpressive.Home.Contracts.Services
{
    public interface IWebHookService
    {
        Task<IWebHook> RegisterNewWebHookAsync(string gatewayName, IDevice device);

        Task<IWebHook> GetWebHookAsync(string id);
    }

    public interface IWebHook
    {
        string Id { get; }
        string GatewayName { get; }
        string DeviceId { get; }
    }
}
