using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xpressive.Home.Contracts;
using Xpressive.Home.Contracts.Automation;

namespace Xpressive.Home.Plugins.Sms
{
    public class SmsPlugin : IPlugin
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<IScriptObjectProvider, SmsScriptObjectProvider>();

            services.AddSingleton<IHostedService, SmsGateway>();
        }
    }
}
