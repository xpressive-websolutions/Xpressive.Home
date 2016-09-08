using Autofac;
using Xpressive.Home.Contracts.Automation;
using Xpressive.Home.Contracts.Gateway;

namespace Xpressive.Home.Plugins.Netatmo
{
    public class NetatmoModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<NetatmoScriptObjectProvider>().As<IScriptObjectProvider>();

            builder.RegisterType<NetatmoGateway>()
                .As<IGateway>()
                .As<INetatmoGateway>()
                .SingleInstance();

            base.Load(builder);
        }
    }
}
