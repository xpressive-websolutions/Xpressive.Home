using Autofac;
using Xpressive.Home.Contracts.Automation;
using Xpressive.Home.Contracts.Gateway;
using Xpressive.Home.Contracts.Messaging;

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
                .As<IMessageQueueListener<CommandMessage>>()
                .As<IMessageQueueListener<NetworkDeviceFoundMessage>>()
                .SingleInstance();

            base.Load(builder);
        }
    }
}
