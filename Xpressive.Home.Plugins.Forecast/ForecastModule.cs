using Autofac;
using Xpressive.Home.Contracts.Gateway;

namespace Xpressive.Home.Plugins.Forecast
{
    public class ForecastModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<ForecastGateway>()
                .As<IGateway>()
                .PropertiesAutowired()
                .SingleInstance();

            base.Load(builder);
        }
    }
}