using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xpressive.Home.Contracts;
using Xpressive.Home.Contracts.Automation;
using Xpressive.Home.Contracts.Gateway;

namespace Xpressive.Home.Plugins.Sonos
{
    public class SonosPlugin : IPlugin
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<IScriptObjectProvider, SonosScriptObjectProvider>();
            services.AddTransient<ISonosSoapClient, SonosSoapClient>();
            services.AddTransient<ISonosDeviceDiscoverer, SonosDeviceDiscoverer>();

            services.AddSingleton<SonosGateway>();
            services.AddSingleton<ISonosGateway>(s => s.GetService<SonosGateway>());
            services.AddSingleton<IHostedService>(s => s.GetService<SonosGateway>());
            services.AddSingleton<IGateway>(s => s.GetService<SonosGateway>());
        }
    }
}
