using Autofac;
using Xpressive.Home.Contracts.Gateway;

namespace Xpressive.Home.Plugins.Tado
{
    public class TadoModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<TadoGateway>()
                .As<IGateway>()
                .SingleInstance();

            base.Load(builder);
        }
    }
}
