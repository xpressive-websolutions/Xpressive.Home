using Autofac;
using Xpressive.Home.Contracts.Gateway;
using Xpressive.Home.Contracts.Messaging;

namespace Xpressive.Home.Plugins.CloudTwin
{
    public class CloudTwinModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<CloudTwinGateway>()
                .As<IGateway>()
                .As<IMessageQueueListener<UpdateVariableMessage>>()
                .PropertiesAutowired()
                .SingleInstance();

            base.Load(builder);
        }
    }
}
