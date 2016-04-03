using Autofac;
using Xpressive.Home.Contracts.Services;

namespace Xpressive.Home.Services
{
    public class ServiceModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<IpAddressService>().As<IIpAddressService>();
            builder.RegisterType<RadioStationService>().As<IRadioStationService>();
            builder.RegisterType<LowBatteryDeviceObserver>().As<IStartable>().SingleInstance();

            builder.RegisterType<UpnpDeviceDiscoveringService>()
                .As<IUpnpDeviceDiscoveringService>()
                .OnActivated(async a => await a.Instance.StartDiscoveringAsync());

            base.Load(builder);
        }
    }
}
