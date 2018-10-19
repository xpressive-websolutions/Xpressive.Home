using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xpressive.Home.Contracts;

namespace Xpressive.Home.Plugins.Tado
{
    public class TadoPlugin : IPlugin
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IHostedService, TadoGateway>();
        }
    }
}
