using Autofac;
using Xpressive.Home.Contracts.Services;

namespace Xpressive.Home.Plugins.ZyxelUsg
{
    public class ZyxelUsgModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<ZyxelUsgDeviceScanner>()
                .As<INetworkDeviceScanner>()
                .SingleInstance();

            base.Load(builder);
        }
    }
}
