using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xpressive.Home.Contracts;
using Xpressive.Home.Contracts.Automation;
using Xpressive.Home.Contracts.Gateway;

namespace Xpressive.Home.Plugins.ForeignExchangeRates
{
    public class ForeignExchangeRatesPlugin : IPlugin
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<IScriptObjectProvider, ForeignExchangeRatesScriptObjectProvider>();

            services.AddSingleton<ForeignExchangeRatesGateway>();
            services.AddSingleton<IForeignExchangeRatesGateway>(s => s.GetService<ForeignExchangeRatesGateway>());
            services.AddSingleton<IHostedService>(s => s.GetService<ForeignExchangeRatesGateway>());
            services.AddSingleton<IGateway>(s => s.GetService<ForeignExchangeRatesGateway>());
        }
    }
}
