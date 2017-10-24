using Autofac;
using Xpressive.Home.Contracts.Gateway;
using Xpressive.Home.Contracts.Messaging;

namespace Xpressive.Home.Plugins.NetworkDeviceAvailability
{
    public class NetworkDeviceAvailabilityModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<NetworkDeviceAvailabilityGateway>()
                .As<IGateway>()
                .As<IMessageQueueListener<NetworkDeviceFoundMessage>>()
                .SingleInstance();

            base.Load(builder);
        }
    }
}
