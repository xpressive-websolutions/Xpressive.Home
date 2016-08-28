using System;
using System.Collections.Generic;
using System.Linq;
using Xpressive.Home.Contracts.Gateway;
using Xpressive.Home.Contracts.Messaging;
using Xpressive.Home.Contracts.Services;
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

            var service = IocContainer.Resolve<ISoftwareUpdateDownloadService>();
            service.DownloadNewestVersionAsync();

            var gateways = IocContainer.Resolve<IList<IGateway>>();
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
            private readonly IEnumerable<IDisposable> _disposables;

            public Disposer(IEnumerable<IDisposable> disposables)
            {
                _disposables = disposables;
            }

            public void Dispose()
            {
                foreach (var disposable in _disposables)
                {
                    disposable.Dispose();
                }
            }
        }
    }
}
