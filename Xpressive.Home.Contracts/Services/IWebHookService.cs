using System.Collections.Generic;
using System.Threading.Tasks;
using Xpressive.Home.Contracts.Gateway;

namespace Xpressive.Home.Contracts.Services
{
    public interface IWebHookService
    {
        Task<IWebHook> RegisterNewWebHookAsync(string gatewayName, IDevice device);

        Task<IWebHook> RegisterNewWebHookAsync(string gatewayName, string id, IDevice device);

        Task<IWebHook> GetWebHookAsync(string id);

        Task<IEnumerable<IWebHook>> GetWebHooksAsync(string gatewayName, string deviceId);

        string GenerateId();
    }

    public interface IWebHook
    {
        string Id { get; }
        string GatewayName { get; }
        string DeviceId { get; }
    }
}
