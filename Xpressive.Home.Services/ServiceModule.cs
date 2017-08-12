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
            builder.RegisterType<DeviceConfigurationBackupService>().As<IDeviceConfigurationBackupService>();
            builder.RegisterType<WebHookService>().As<IWebHookService>();
            builder.RegisterType<Base62Converter>().As<IBase62Converter>();
            builder.RegisterType<HttpClientProvider>().As<IHttpClientProvider>().SingleInstance();

            builder.RegisterType<SoftwareUpdateDownloadService>()
                .As<ISoftwareUpdateDownloadService>()
                .As<IStartable>()
                .SingleInstance();

            builder.RegisterType<UpnpDeviceDiscoveringService>()
                .As<INetworkDeviceScanner>()
                .SingleInstance();

            base.Load(builder);
        }
    }
}
