using Autofac;
using Xpressive.Home.Contracts.Gateway;

namespace Xpressive.Home.Plugins.Gardena
{
    internal class GardenaModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<GardenaGateway>()
                .As<IGateway>()
                .PropertiesAutowired()
                .SingleInstance();

            base.Load(builder);
        }
    }
}
