using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xpressive.Home.Contracts;
using Xpressive.Home.Contracts.Automation;

namespace Xpressive.Home.Plugins.MyStrom
{
    public class MyStromPlugin : IPlugin
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<IScriptObjectProvider, MyStromScriptObjectProvider>();
            services.AddTransient<IMyStromDeviceNameService, MyStromDeviceNameService>();

            services.AddSingleton<MyStromGateway>();
            services.AddSingleton<IMyStromGateway>(s => s.GetService<MyStromGateway>());
            services.AddSingleton<IHostedService>(s => s.GetService<MyStromGateway>());
        }
    }
}
