using Autofac;
using Xpressive.Home.Contracts.Automation;
using Xpressive.Home.Contracts.Gateway;
using Xpressive.Home.Contracts.Messaging;

namespace Xpressive.Home.Plugins.Lifx
{
    public class LifxModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<LifxScriptObjectProvider>().As<IScriptObjectProvider>();

            builder.RegisterType<LifxGateway>()
                .As<IGateway>()
                .As<ILifxGateway>()
                .As<IMessageQueueListener<CommandMessage>>()
                .SingleInstance();

            base.Load(builder);
        }
    }
}
