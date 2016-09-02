using Autofac;
using Xpressive.Home.Contracts.Automation;
using Xpressive.Home.Contracts.Gateway;

namespace Xpressive.Home.Plugins.Daylight
{
    public class DaylightModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<DaylightScriptObjectProvider>().As<IScriptObjectProvider>();

            builder.RegisterType<DaylightGateway>()
                .As<IGateway>()
                .As<IDaylightGateway>()
                .PropertiesAutowired()
                .SingleInstance();

            base.Load(builder);
        }
    }
}
