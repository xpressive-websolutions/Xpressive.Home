using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xpressive.Home.Contracts;

namespace Xpressive.Home.Plugins.Gardena
{
    internal class GardenaPlugin : IPlugin
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IHostedService, GardenaGateway>();
        }
    }
}
