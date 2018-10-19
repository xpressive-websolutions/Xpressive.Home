using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xpressive.Home.Contracts;
using Xpressive.Home.Contracts.Automation;

namespace Xpressive.Home.Plugins.NissanLeaf
{
    public sealed class NissanLeafPlugin : IPlugin
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<IScriptObjectProvider, NissanLeafScriptObjectProvider>();
            services.AddTransient<IBlowfishEncryptionService, BlowfishEncryptionService>();
            services.AddTransient<INissanLeafClient, NissanLeafClient>();

            services.AddSingleton<NissanLeafGateway>();
            services.AddSingleton<INissanLeafGateway>(s => s.GetService<NissanLeafGateway>());
            services.AddSingleton<IHostedService>(s => s.GetService<NissanLeafGateway>());
        }
    }
}
