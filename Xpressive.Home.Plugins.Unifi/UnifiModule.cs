using Autofac;
using Xpressive.Home.Contracts.Services;

namespace Xpressive.Home.Plugins.Unifi
{
    public class UnifiModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<UnifiDeviceScanner>()
                .As<INetworkDeviceScanner>()
                .SingleInstance();

            base.Load(builder);
        }
    }
}
