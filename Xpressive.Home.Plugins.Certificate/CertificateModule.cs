using Autofac;
using Xpressive.Home.Contracts.Automation;
using Xpressive.Home.Contracts.Gateway;

namespace Xpressive.Home.Plugins.Certificate
{
    public class CertificateModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<CertificateScriptObjectProvider>().As<IScriptObjectProvider>();

            builder.RegisterType<CertificateGateway>()
                .As<IGateway>()
                .As<ICertificateGateway>()
                .PropertiesAutowired()
                .SingleInstance();

            base.Load(builder);
        }
    }
}
