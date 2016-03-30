using Autofac;
using Xpressive.Home.Contracts.Gateway;

namespace Xpressive.Home.Plugins.Sonos
{
    public class SonosModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<SonosSoapClient>().As<ISonosSoapClient>();
            builder.RegisterType<SonosDeviceDiscoverer>().As<ISonosDeviceDiscoverer>();
            builder.RegisterType<SonosGateway>().As<IGateway>().SingleInstance();

            base.Load(builder);
        }
    }
}
