using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xpressive.Home.Contracts;
using Xpressive.Home.Contracts.Automation;

namespace Xpressive.Home.Plugins.PhilipsHue
{
    public class PhilipsHuePlugin : IPlugin
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<IScriptObjectProvider, PhilipsHueScriptObjectProvider>();
            services.AddSingleton<IPhilipsHueDeviceDiscoveringService, PhilipsHueDeviceDiscoveringService>();
            services.AddSingleton<IPhilipsHueBridgeDiscoveringService, PhilipsHueBridgeDiscoveringService>();

            services.AddSingleton<PhilipsHueGateway>();
            services.AddSingleton<IPhilipsHueGateway>(s => s.GetService<PhilipsHueGateway>());
            services.AddSingleton<IHostedService>(s => s.GetService<PhilipsHueGateway>());
        }
    }
}
