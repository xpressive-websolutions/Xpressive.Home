using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xpressive.Home.Contracts;
using Xpressive.Home.Contracts.Automation;

namespace Xpressive.Home.Plugins.Lifx
{
    public class LifxPlugin : IPlugin
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<IScriptObjectProvider, LifxScriptObjectProvider>();

            services.AddSingleton<LifxGateway>();
            services.AddSingleton<ILifxGateway>(s => s.GetService<LifxGateway>());
            services.AddSingleton<IHostedService>(s => s.GetService<LifxGateway>());
        }
    }
}
