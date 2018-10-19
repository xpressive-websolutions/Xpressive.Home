using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xpressive.Home.Contracts;
using Xpressive.Home.Contracts.Automation;
using Xpressive.Home.Contracts.Gateway;

namespace Xpressive.Home.Plugins.Workday
{
    public class WorkdayPlugin : IPlugin
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<IScriptObjectProvider, WorkdayScriptObjectProvider>();
            services.AddTransient<IWorkdayCalculator, WorkdayCalculator>();

            services.AddSingleton<WorkdayGateway>();
            services.AddSingleton<IWorkdayGateway>(s => s.GetService<WorkdayGateway>());
            services.AddSingleton<IHostedService>(s => s.GetService<WorkdayGateway>());
            services.AddSingleton<IGateway>(s => s.GetService<WorkdayGateway>());
        }
    }
}
