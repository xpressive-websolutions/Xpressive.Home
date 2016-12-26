using Autofac;
using Xpressive.Home.Contracts.Automation;
using Xpressive.Home.Contracts.Gateway;

namespace Xpressive.Home.Plugins.Pushover
{
    public class PushoverModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<PushoverScriptObjectProvider>().As<IScriptObjectProvider>();

            builder
                .RegisterType<PushoverGateway>()
                .As<IGateway>()
                .SingleInstance();

            base.Load(builder);
        }
    }
}
