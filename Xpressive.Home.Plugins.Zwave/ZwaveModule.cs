using Autofac;
using Xpressive.Home.Contracts.Gateway;
using Xpressive.Home.Plugins.Zwave.CommandClassHandlers;

namespace Xpressive.Home.Plugins.Zwave
{
    public class ZwaveModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<AlarmCommandClassHandler>().As<ICommandClassHandler>();
            builder.RegisterType<BasicCommandClassHandler>().As<ICommandClassHandler>();
            builder.RegisterType<BatteryCommandClassHandler>().As<ICommandClassHandler>();
            builder.RegisterType<MeterCommandClassHandler>().As<ICommandClassHandler>();
            builder.RegisterType<SensorAlarmCommandClassHandler>().As<ICommandClassHandler>();
            builder.RegisterType<SensorBinaryCommandClassHandler>().As<ICommandClassHandler>();
            builder.RegisterType<SensorMultiLevelCommandClassHandler>().As<ICommandClassHandler>();
            builder.RegisterType<SwitchBinaryCommandClassHandler>().As<ICommandClassHandler>();
            builder.RegisterType<WakeUpCommandClassHandler>().As<ICommandClassHandler>();

            builder.RegisterType<ZwaveGateway>()
                .As<IGateway>()
                .SingleInstance()
                .OnActivating(async g => await g.Instance.Start());

            base.Load(builder);
        }
    }
}
