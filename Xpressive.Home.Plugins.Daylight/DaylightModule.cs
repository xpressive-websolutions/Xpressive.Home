using Autofac;
using Xpressive.Home.Contracts.Gateway;

namespace Xpressive.Home.Plugins.Daylight
{
    public class DaylightModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<DaylightGateway>().As<IGateway>().SingleInstance();

            base.Load(builder);
        }
    }
}