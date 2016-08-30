using Autofac;
using Xpressive.Home.Contracts.Automation;
using Xpressive.Home.Contracts.Gateway;

namespace Xpressive.Home.Plugins.Pushalot
{
    public class PushalotModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<PushalotScriptObjectProvider>().As<IScriptObjectProvider>();

            builder
                .RegisterType<PushalotGateway>()
                .As<IGateway>()
                .SingleInstance();

            base.Load(builder);
        }
    }
}
