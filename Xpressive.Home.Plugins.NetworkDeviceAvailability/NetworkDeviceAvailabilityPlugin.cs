using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xpressive.Home.Contracts;

namespace Xpressive.Home.Plugins.NetworkDeviceAvailability
{
    public class NetworkDeviceAvailabilityPlugin : IPlugin
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IHostedService, NetworkDeviceAvailabilityGateway>();
        }
    }
}
