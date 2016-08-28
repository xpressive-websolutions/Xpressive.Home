using Autofac;
using Xpressive.Home.Contracts.Gateway;
using Xpressive.Home.Contracts.Rooms;
using Xpressive.Home.Contracts.Services;
using Module = Autofac.Module;

namespace Xpressive.Home.Services
{
    public class ServiceModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<IpAddressService>().As<IIpAddressService>();
            builder.RegisterType<TuneInRadioStationService>().As<ITuneInRadioStationService>();
            builder.RegisterType<LowBatteryDeviceObserver>().As<IStartable>().SingleInstance();
            builder.RegisterType<DevicePersistingService>().As<IDevicePersistingService>();
            builder.RegisterType<RoomRepository>().As<IRoomRepository>();
            builder.RegisterType<RoomScriptGroupRepository>().As<IRoomScriptGroupRepository>();
            builder.RegisterType<RoomScriptRepository>().As<IRoomScriptRepository>();
            builder.RegisterType<FavoriteRadioStationService>().As<IFavoriteRadioStationService>();
            builder.RegisterType<RoomDeviceService>().As<IRoomDeviceService>();
            builder.RegisterType<SoftwareUpdateDownloadService>().As<ISoftwareUpdateDownloadService>();

            builder.RegisterType<UpnpDeviceDiscoveringService>()
                .As<IUpnpDeviceDiscoveringService>()
                .OnActivated(async a => await a.Instance.StartDiscoveringAsync());

            base.Load(builder);
        }
    }
}
