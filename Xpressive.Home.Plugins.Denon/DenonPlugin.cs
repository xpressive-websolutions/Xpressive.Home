using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xpressive.Home.Contracts;
using Xpressive.Home.Contracts.Automation;

namespace Xpressive.Home.Plugins.Denon
{
    public class DenonPlugin : IPlugin
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<IScriptObjectProvider, DenonScriptObjectProvider>();

            services.AddSingleton<DenonGateway>();
            services.AddSingleton<IDenonGateway>(s => s.GetService<DenonGateway>());
            services.AddSingleton<IHostedService>(s => s.GetService<DenonGateway>());
        }
    }
}
