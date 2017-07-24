using Autofac;
using Xpressive.Home.Contracts.Automation;
using Xpressive.Home.Contracts.Gateway;

namespace Xpressive.Home.Plugins.Workday
{
    public class WorkdayModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<WorkdayGateway>()
                .As<IGateway>()
                .As<IWorkdayGateway>()
                .PropertiesAutowired()
                .SingleInstance();

            builder.RegisterType<WorkdayScriptObjectProvider>().As<IScriptObjectProvider>();
            builder.RegisterType<WorkdayCalculator>().As<IWorkdayCalculator>();

            base.Load(builder);
        }
    }
}
