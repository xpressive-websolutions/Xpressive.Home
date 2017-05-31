using Autofac;
using Xpressive.Home.Contracts.Gateway;

namespace Xpressive.Home.Plugins.NetworkDeviceAvailability
{
    public class NetworkDeviceAvailabilityModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<NetworkDeviceAvailabilityGateway>()
                .As<IGateway>()
                .SingleInstance();

            base.Load(builder);
        }
    }
}
