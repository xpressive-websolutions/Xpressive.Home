using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xpressive.Home.Contracts;
using Xpressive.Home.Contracts.Gateway;

namespace Xpressive.Home.Plugins.Gardena
{
    internal class GardenaPlugin : IPlugin
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<GardenaGateway>();
            services.AddSingleton<IHostedService>(s => s.GetService<GardenaGateway>());
            services.AddSingleton<IGateway>(s => s.GetService<GardenaGateway>());
        }
    }
}
