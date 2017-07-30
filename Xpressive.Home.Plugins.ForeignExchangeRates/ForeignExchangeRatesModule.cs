using Autofac;
using Xpressive.Home.Contracts.Automation;
using Xpressive.Home.Contracts.Gateway;

namespace Xpressive.Home.Plugins.ForeignExchangeRates
{
    public class ForeignExchangeRatesModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<ForeignExchangeRatesScriptObjectProvider>().As<IScriptObjectProvider>();

            builder.RegisterType<ForeignExchangeRatesGateway>()
                .As<IGateway>()
                .As<IForeignExchangeRatesGateway>()
                .PropertiesAutowired()
                .SingleInstance();

            base.Load(builder);
        }
    }
}
