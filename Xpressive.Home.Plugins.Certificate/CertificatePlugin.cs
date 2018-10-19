using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xpressive.Home.Contracts;
using Xpressive.Home.Contracts.Automation;

namespace Xpressive.Home.Plugins.Certificate
{
    //public class CertificateModule : Module
    //{
    //    protected override void Load(ContainerBuilder builder)
    //    {
    //        builder.RegisterType<CertificateScriptObjectProvider>().As<IScriptObjectProvider>();

    //        builder.RegisterType<CertificateGateway>()
    //            .As<IGateway>()
    //            .As<ICertificateGateway>()
    //            .PropertiesAutowired()
    //            .SingleInstance();

    //        base.Load(builder);
    //    }
    //}

    public class CertificatePlugin : IPlugin
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<IScriptObjectProvider, CertificateScriptObjectProvider>();

            services.AddSingleton<CertificateGateway>();
            services.AddSingleton<ICertificateGateway>(s => s.GetService<CertificateGateway>());
            services.AddSingleton<IHostedService>(s => s.GetService<CertificateGateway>());
        }
    }
}
