using Autofac;
using Xpressive.Home.Contracts.Automation;

namespace Xpressive.Home.Plugins.Pushalot
{
    public class PushalotModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<PushalotScriptObjectProvider>().As<IScriptObjectProvider>();

            base.Load(builder);
        }
    }
}
