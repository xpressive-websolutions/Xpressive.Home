using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xpressive.Home.Contracts;
using Xpressive.Home.Contracts.Automation;
using Xpressive.Home.Contracts.Gateway;

namespace Xpressive.Home.Plugins.Daylight
{
    public class DaylightPlugin : IPlugin
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<IScriptObjectProvider, DaylightScriptObjectProvider>();

            services.AddSingleton<DaylightGateway>();
            services.AddSingleton<IDaylightGateway>(s => s.GetService<DaylightGateway>());
            services.AddSingleton<IHostedService>(s => s.GetService<DaylightGateway>());
            services.AddSingleton<IGateway>(s => s.GetService<DaylightGateway>());
        }
    }
}
