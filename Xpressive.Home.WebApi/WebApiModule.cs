using System.Reflection;
using Autofac;
using Autofac.Integration.WebApi;
using Xpressive.Home.Contracts.Messaging;
using Xpressive.Home.WebApi.Controllers;
using Module = Autofac.Module;

namespace Xpressive.Home.WebApi
{
    public class WebApiModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<WebApiStartable>().As<IStartable>().SingleInstance();

            builder.RegisterType<UserNotificationHub>()
                .As<IMessageQueueListener<NotifyUserMessage>>()
                .SingleInstance();

            var executingAssembly = Assembly.GetExecutingAssembly();
            builder.RegisterApiControllers(executingAssembly);

            base.Load(builder);
        }
    }
}
