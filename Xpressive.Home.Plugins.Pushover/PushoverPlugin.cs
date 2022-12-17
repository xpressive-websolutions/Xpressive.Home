using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xpressive.Home.Contracts;
using Xpressive.Home.Contracts.Automation;
using Xpressive.Home.Contracts.Gateway;

namespace Xpressive.Home.Plugins.Pushover
{
    public class PushoverPlugin : IPlugin
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<IScriptObjectProvider, PushoverScriptObjectProvider>();

            services.AddSingleton<PushoverGateway>();
            services.AddSingleton<IHostedService>(s => s.GetService<PushoverGateway>());
            services.AddSingleton<IGateway>(s => s.GetService<PushoverGateway>());
        }
    }
}
