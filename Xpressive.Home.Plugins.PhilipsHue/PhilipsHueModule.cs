using Autofac;
using Xpressive.Home.Contracts.Gateway;

namespace Xpressive.Home.Plugins.PhilipsHue
{
    public class PhilipsHueModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<PhilipsHueGateway>()
                .As<IGateway>()
                .SingleInstance()
                .OnActivated(async h => await h.Instance.ObserveBulbStatusAsync());

            builder.RegisterType<PhilipsHueDeviceDiscoveringService>()
                .As<IPhilipsHueDeviceDiscoveringService>()
                .SingleInstance();

            builder.RegisterType<PhilipsHueBridgeDiscoveringService>()
                .As<IPhilipsHueBridgeDiscoveringService>()
                .SingleInstance();

            base.Load(builder);
        }
    }
}
