using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xpressive.Home.Contracts;
using Xpressive.Home.Contracts.Automation;
using Xpressive.Home.Contracts.Gateway;

namespace Xpressive.Home.Plugins.Certificate
{
    public class CertificatePlugin : IPlugin
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<IScriptObjectProvider, CertificateScriptObjectProvider>();

            services.AddSingleton<CertificateGateway>();
            services.AddSingleton<ICertificateGateway>(s => s.GetService<CertificateGateway>());
            services.AddSingleton<IHostedService>(s => s.GetService<CertificateGateway>());
            services.AddSingleton<IGateway>(s => s.GetService<CertificateGateway>());
        }
    }
}
