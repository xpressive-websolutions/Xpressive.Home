using Autofac;
using Xpressive.Home.Contracts.Gateway;
using Xpressive.Home.Contracts.Messaging;

namespace Xpressive.Home.Plugins.Tado
{
    public class TadoModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<TadoGateway>()
                .As<IGateway>()
                .As<IMessageQueueListener<CommandMessage>>()
                .SingleInstance();

            base.Load(builder);
        }
    }
}
