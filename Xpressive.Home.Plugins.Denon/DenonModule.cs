using Autofac;
using Xpressive.Home.Contracts.Gateway;

namespace Xpressive.Home.Plugins.Denon
{
    public class DenonModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<DenonGateway>().As<IGateway>().SingleInstance();

            base.Load(builder);
        }
    }
}
