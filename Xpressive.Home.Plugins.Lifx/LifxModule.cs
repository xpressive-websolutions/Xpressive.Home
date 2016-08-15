using Autofac;
using Xpressive.Home.Contracts.Automation;
using Xpressive.Home.Contracts.Gateway;

namespace Xpressive.Home.Plugins.Lifx
{
    public class LifxModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<LifxScriptObjectProvider>().As<IScriptObjectProvider>();

            builder.RegisterType<LifxGateway>()
                .As<IGateway>()
                .As<ILifxGateway>()
                .SingleInstance()
                .OnActivated(async h => await h.Instance.FindBulbsAsync());

            base.Load(builder);
        }
    }
}
