using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xpressive.Home.Contracts;
using Xpressive.Home.Contracts.Gateway;

namespace Xpressive.Home.Plugins.Forecast
{
    public class ForecastPlugin : IPlugin
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<ForecastGateway>();
            services.AddSingleton<IHostedService>(s => s.GetService<ForecastGateway>());
            services.AddSingleton<IGateway>(s => s.GetService<ForecastGateway>());
        }
    }
}