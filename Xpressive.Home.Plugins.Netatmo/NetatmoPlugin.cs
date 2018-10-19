using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xpressive.Home.Contracts;
using Xpressive.Home.Contracts.Automation;

namespace Xpressive.Home.Plugins.Netatmo
{
    public class NetatmoPlugin : IPlugin
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<IScriptObjectProvider, NetatmoScriptObjectProvider>();

            services.AddSingleton<NetatmoGateway>();
            services.AddSingleton<INetatmoGateway>(s => s.GetService<NetatmoGateway>());
            services.AddSingleton<IHostedService>(s => s.GetService<NetatmoGateway>());
        }
    }
}
