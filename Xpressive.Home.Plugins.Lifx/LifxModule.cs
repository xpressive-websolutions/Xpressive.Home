using Autofac;
using Xpressive.Home.Contracts.Gateway;

namespace Xpressive.Home.Plugins.Lifx
{
    public class LifxModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<LifxGateway>()
                .As<IGateway>()
                .SingleInstance()
                .OnActivating(async h => await h.Instance.FindBulbsAsync());

            base.Load(builder);
        }
    }
}