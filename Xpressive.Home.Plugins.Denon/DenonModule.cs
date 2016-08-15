using Autofac;
using Xpressive.Home.Contracts.Automation;
using Xpressive.Home.Contracts.Gateway;

namespace Xpressive.Home.Plugins.Denon
{
    public class DenonModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<DenonScriptObjectProvider>().As<IScriptObjectProvider>();

            builder.RegisterType<DenonGateway>()
                .As<IGateway>()
                .As<IDenonGateway>()
                .SingleInstance();

            base.Load(builder);
        }
    }
}
