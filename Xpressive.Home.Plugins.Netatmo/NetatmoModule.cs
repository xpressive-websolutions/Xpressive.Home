using Autofac;
using Xpressive.Home.Contracts.Gateway;

namespace Xpressive.Home.Plugins.Netatmo
{
    public class NetatmoModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<NetatmoGateway>()
                .As<IGateway>()
                .SingleInstance();

            base.Load(builder);
        }
    }
}
