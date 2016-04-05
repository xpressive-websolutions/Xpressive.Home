using Autofac;
using Xpressive.Home.Contracts.Gateway;

namespace Xpressive.Home.Plugins.Forecast
{
    public class ForegastModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<ForecastGateway>()
                .As<IGateway>()
                .SingleInstance()
                .OnActivated(async h => await h.Instance.StartAsync());

            base.Load(builder);
        }
    }
}