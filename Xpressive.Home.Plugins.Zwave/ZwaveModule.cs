using Autofac;
using Xpressive.Home.Contracts.Gateway;

namespace Xpressive.Home.Plugins.Zwave
{
    public class ZwaveModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<ZwaveGateway>()
                .As<IGateway>()
                .SingleInstance()
                .OnActivating(async g => await g.Instance.Start());

            base.Load(builder);
        }
    }
}
