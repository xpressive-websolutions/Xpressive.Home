using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Xpressive.Home.Contracts.Gateway
{
    public interface IDevicePersistingService
    {
        Task SaveAsync(string gatewayName, DeviceBase device);

        Task DeleteAsync(string gatewayName, DeviceBase device);

        Task<IEnumerable<DeviceBase>> GetAsync(string gatewayName, Func<string, string, DeviceBase> emptyDevice);
    }
}
