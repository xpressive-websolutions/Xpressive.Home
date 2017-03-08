using Autofac;
using Xpressive.Home.Contracts.Gateway;
using Xpressive.Home.Contracts.Messaging;

namespace Xpressive.Home.Plugins.Zwave
{
    public class ZwaveModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<ZwaveGateway>()
                .As<IGateway>()
                .As<IMessageQueueListener<CommandMessage>>()
                .SingleInstance();

            base.Load(builder);
        }
    }
}
