using Autofac;
using Xpressive.Home.Contracts.Gateway;

namespace Xpressive.Home.Plugins.WebHook
{
    public class WebHookModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<WebHookGateway>()
                .As<IGateway>()
                .PropertiesAutowired()
                .SingleInstance();

            base.Load(builder);
        }
    }
}
