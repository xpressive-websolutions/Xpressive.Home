using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xpressive.Home.Contracts;
using Xpressive.Home.Contracts.Gateway;

namespace Xpressive.Home.Plugins.Zwave
{
    public class ZwavePlugin : IPlugin
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<ZwaveGateway>();
            services.AddSingleton<IHostedService>(s => s.GetService<ZwaveGateway>());
            services.AddSingleton<IGateway>(s => s.GetService<ZwaveGateway>());
        }
    }
}
