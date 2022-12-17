using Microsoft.Extensions.DependencyInjection;
using Xpressive.Home.Contracts;

namespace Xpressive.Home.Plugins.Unifi
{
    public class UnifiPlugin : IPlugin
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHostedService<UnifiDeviceScanner>();
        }
    }
}
