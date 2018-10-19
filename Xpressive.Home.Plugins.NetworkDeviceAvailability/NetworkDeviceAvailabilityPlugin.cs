using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xpressive.Home.Contracts;
using Xpressive.Home.Contracts.Gateway;

namespace Xpressive.Home.Plugins.NetworkDeviceAvailability
{
    public class NetworkDeviceAvailabilityPlugin : IPlugin
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<NetworkDeviceAvailabilityGateway>();
            services.AddSingleton<IHostedService>(s => s.GetService<NetworkDeviceAvailabilityGateway>());
            services.AddSingleton<IGateway>(s => s.GetService<NetworkDeviceAvailabilityGateway>());
        }
    }
}
