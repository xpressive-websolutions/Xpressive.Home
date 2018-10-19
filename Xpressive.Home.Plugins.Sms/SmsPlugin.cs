using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xpressive.Home.Contracts;
using Xpressive.Home.Contracts.Automation;
using Xpressive.Home.Contracts.Gateway;

namespace Xpressive.Home.Plugins.Sms
{
    public class SmsPlugin : IPlugin
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<IScriptObjectProvider, SmsScriptObjectProvider>();

            services.AddSingleton<SmsGateway>();
            services.AddSingleton<IHostedService>(s => s.GetService<SmsGateway>());
            services.AddSingleton<IGateway>(s => s.GetService<SmsGateway>());
        }
    }
}
