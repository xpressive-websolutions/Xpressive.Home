using Autofac;
using Quartz.Spi;
using Xpressive.Home.Automation;
using Xpressive.Home.Contracts.Automation;
using Xpressive.Home.Contracts.Messaging;
using Xpressive.Home.Contracts.Variables;
using Xpressive.Home.Messaging;
using Xpressive.Home.Variables;

namespace Xpressive.Home
{
    internal class MainModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<MessageQueue>().As<IMessageQueue>().SingleInstance();
            builder.RegisterType<VariablePersistingService>().As<IVariablePersistingService>().SingleInstance();
            builder.RegisterType<RecurrentScriptJobFactory>().As<IJobFactory>();
            builder.RegisterType<ScheduledScriptRepository>().As<IScheduledScriptRepository>();
            builder.RegisterType<ScriptRepository>().As<IScriptRepository>();
            builder.RegisterType<VariableScriptObjectProvider>().As<IScriptObjectProvider>();
            builder.RegisterType<DefaultScriptObjectProvider>().As<IScriptObjectProvider>();
            builder.RegisterType<SchedulerScriptObjectProvider>().As<IScriptObjectProvider>();
            builder.RegisterType<ScriptTriggerService>().As<IScriptTriggerService>();

            builder.RegisterType<ScriptEngine>()
                .As<IScriptEngine>()
                .As<IMessageQueueListener<ExecuteScriptMessage>>()
                .SingleInstance();

            builder.RegisterType<MessageQueueScriptTriggerListener>()
                .As<IMessageQueueListener<UpdateVariableMessage>>()
                .As<IStartable>()
                .SingleInstance();

            builder.RegisterType<MessageQueueLogListener>()
                .As<IMessageQueueListener<UpdateVariableMessage>>()
                .As<IMessageQueueListener<NotifyUserMessage>>()
                .As<IMessageQueueListener<CommandMessage>>()
                .As<IMessageQueueListener<ExecuteScriptMessage>>()
                .As<IMessageQueueListener<NetworkDeviceFoundMessage>>()
                .SingleInstance();

            builder.RegisterType<RenameDeviceListener>()
                .As<IMessageQueueListener<UpdateVariableMessage>>()
                .SingleInstance();

            builder.RegisterType<VariableRepository>()
                .As<IVariableRepository>()
                .As<IMessageQueueListener<UpdateVariableMessage>>()
                .As<IStartable>()
                .SingleInstance();

            builder.RegisterType<VariableHistoryService>()
                .As<IVariableHistoryService>()
                .As<IMessageQueueListener<UpdateVariableMessage>>()
                .SingleInstance();

            builder.RegisterType<CronService>()
                .As<ICronService>()
                .As<IStartable>()
                .SingleInstance();

            base.Load(builder);
        }
    }
}
