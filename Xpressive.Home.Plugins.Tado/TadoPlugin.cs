using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xpressive.Home.Contracts;
using Xpressive.Home.Contracts.Gateway;

namespace Xpressive.Home.Plugins.Tado
{
    public class TadoPlugin : IPlugin
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<TadoGateway>();
            services.AddSingleton<IHostedService>(s => s.GetService<TadoGateway>());
            services.AddSingleton<IGateway>(s => s.GetService<TadoGateway>());
        }
    }
}
