using Autofac;
using Xpressive.Home.Contracts.Automation;
using Xpressive.Home.Contracts.Gateway;
using Xpressive.Home.Contracts.Messaging;

namespace Xpressive.Home.Plugins.Sonos
{
    public class SonosModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<SonosScriptObjectProvider>().As<IScriptObjectProvider>();
            builder.RegisterType<SonosSoapClient>().As<ISonosSoapClient>();
            builder.RegisterType<SonosDeviceDiscoverer>().As<ISonosDeviceDiscoverer>();

            builder.RegisterType<SonosGateway>()
                .As<IGateway>()
                .As<ISonosGateway>()
                .As<IMessageQueueListener<CommandMessage>>()
                .SingleInstance();

            base.Load(builder);
        }
    }
}
