using System.Reflection;
using Autofac;
using Autofac.Integration.WebApi;
using Module = Autofac.Module;

namespace Xpressive.Home.WebApi
{
    public class WebApiModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<WebApiStartable>().As<IStartable>().SingleInstance();
            var executingAssembly = Assembly.GetExecutingAssembly();
            builder.RegisterApiControllers(executingAssembly);

            base.Load(builder);
        }
    }
}