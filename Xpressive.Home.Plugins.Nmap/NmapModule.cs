using Autofac;
using Xpressive.Home.Contracts.Services;

namespace Xpressive.Home.Plugins.Nmap
{
    public class NmapModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<NmapDeviceScanner>()
                .As<INetworkDeviceScanner>()
                .SingleInstance();

            base.Load(builder);
        }
    }
}
