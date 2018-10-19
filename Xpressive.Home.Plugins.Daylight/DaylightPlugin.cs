using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xpressive.Home.Contracts;
using Xpressive.Home.Contracts.Automation;

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
        }
    }
}
