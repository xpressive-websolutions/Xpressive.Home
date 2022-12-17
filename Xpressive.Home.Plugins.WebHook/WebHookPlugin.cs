using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xpressive.Home.Contracts;
using Xpressive.Home.Contracts.Gateway;

namespace Xpressive.Home.Plugins.WebHook
{
    public class WebHookPlugin : IPlugin
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<WebHookGateway>();
            services.AddSingleton<IHostedService>(s => s.GetService<WebHookGateway>());
            services.AddSingleton<IGateway>(s => s.GetService<WebHookGateway>());
        }
    }
}
