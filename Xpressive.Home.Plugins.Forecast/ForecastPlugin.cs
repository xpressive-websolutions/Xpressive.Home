using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xpressive.Home.Contracts;

namespace Xpressive.Home.Plugins.Forecast
{
    public class ForecastPlugin : IPlugin
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IHostedService, ForecastGateway>();
        }
    }
}