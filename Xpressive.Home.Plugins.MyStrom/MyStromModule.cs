using Autofac;
using Xpressive.Home.Contracts.Automation;
using Xpressive.Home.Contracts.Gateway;
using Xpressive.Home.Contracts.Messaging;

namespace Xpressive.Home.Plugins.MyStrom
{
    public class MyStromModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<MyStromDeviceNameService>().As<IMyStromDeviceNameService>();
            builder.RegisterType<MyStromScriptObjectProvider>().As<IScriptObjectProvider>();

            builder.RegisterType<MyStromGateway>()
                .As<IGateway>()
                .As<IMyStromGateway>()
                .As<IMessageQueueListener<CommandMessage>>()
                .As<IMessageQueueListener<NetworkDeviceFoundMessage>>()
                .SingleInstance();

            base.Load(builder);
        }
    }
}
