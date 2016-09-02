using Autofac;
using Xpressive.Home.Contracts.Automation;
using Xpressive.Home.Contracts.Gateway;

namespace Xpressive.Home.Plugins.Sms
{
    public class SmsModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<SmsScriptObjectProvider>().As<IScriptObjectProvider>();

            builder
                .RegisterType<SmsGateway>()
                .As<IGateway>()
                .SingleInstance();

            base.Load(builder);
        }
    }
}
