using Autofac;
using Xpressive.Home.Contracts.Automation;
using Xpressive.Home.Contracts.Gateway;

namespace Xpressive.Home.Plugins.NissanLeaf
{
    public sealed class NissanLeafModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<BlowfishEncryptionService>().As<IBlowfishEncryptionService>();
            builder.RegisterType<NissanLeafClient>().As<INissanLeafClient>();
            builder.RegisterType<NissanLeafScriptObjectProvider>().As<IScriptObjectProvider>();

            builder
                .RegisterType<NissanLeafGateway>()
                .As<IGateway>()
                .As<INissanLeafGateway>()
                .SingleInstance();

            base.Load(builder);
        }
    }
}
