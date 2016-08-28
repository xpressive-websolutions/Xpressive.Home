using System;
using System.Collections.Generic;
using System.Linq;
using Xpressive.Home.Contracts.Gateway;
using Xpressive.Home.Contracts.Messaging;
using Xpressive.Home.DatabaseMigrator;

namespace Xpressive.Home
{
    public static class Setup
    {
        public static IDisposable Run()
        {
            DbMigrator.Run();
            IocContainer.Build();
            RegisterMessageQueueListeners();

            var gateways = IocContainer.Resolve<IList<IGateway>>();

            foreach (var gateway in gateways)
            {
                gateway.StartAsync();
            }

            return new Disposer(gateways);
        }

        private static void RegisterMessageQueueListeners()
        {
            var updateVariableListener = IocContainer.Resolve<IList<IMessageQueueListener<UpdateVariableMessage>>>().ToList();
            var commandMessageListener = IocContainer.Resolve<IList<IMessageQueueListener<CommandMessage>>>().ToList();
            var lowBatteryListener = IocContainer.Resolve<IList<IMessageQueueListener<LowBatteryMessage>>>().ToList();
            var notifyUserListener = IocContainer.Resolve<IList<IMessageQueueListener<NotifyUserMessage>>>().ToList();
            var messageQueue = IocContainer.Resolve<IMessageQueue>();

            updateVariableListener.ForEach(l => messageQueue.Subscribe<UpdateVariableMessage>(l.Notify));
            commandMessageListener.ForEach(l => messageQueue.Subscribe<CommandMessage>(l.Notify));
            lowBatteryListener.ForEach(l => messageQueue.Subscribe<LowBatteryMessage>(l.Notify));
            notifyUserListener.ForEach(l => messageQueue.Subscribe<NotifyUserMessage>(l.Notify));
        }

        private class Disposer : IDisposable
        {
            private readonly IEnumerable<IGateway> _gateways;

            public Disposer(IEnumerable<IGateway> gateways)
            {
                _gateways = gateways;
            }

            public void Dispose()
            {
                foreach (var gateway in _gateways)
                {
                    gateway.Stop();
                    gateway.Dispose();
                }

                IocContainer.Dispose();
            }
        }
    }
}
