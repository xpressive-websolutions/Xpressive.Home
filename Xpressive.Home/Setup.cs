using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Xpressive.Home.Contracts.Gateway;
using Xpressive.Home.Contracts.Messaging;
using Xpressive.Home.Contracts.Services;
using Xpressive.Home.DatabaseMigrator;

namespace Xpressive.Home
{
    public static class Setup
    {
        public static IDisposable Run(IConfiguration configuration)
        {
            DbMigrator.Run(configuration.GetConnectionString("ConnectionString"));
            IocContainer.Build(configuration);
            RegisterMessageQueueListeners();

            var gateways = IocContainer.Resolve<IList<IGateway>>();
            var networkScanners = IocContainer.Resolve<IList<INetworkDeviceScanner>>();
            var cancellationToken = new CancellationTokenSource();

            foreach (var gateway in gateways)
            {
                Task.Factory.StartNew(() => gateway.StartAsync(cancellationToken.Token), TaskCreationOptions.LongRunning);
            }

            foreach (var networkDeviceScanner in networkScanners)
            {
                Task.Factory.StartNew(() => networkDeviceScanner.StartAsync(cancellationToken.Token), TaskCreationOptions.LongRunning);
            }

            return new Disposer(cancellationToken);
        }

        private static void RegisterMessageQueueListeners()
        {
            var messageQueue = IocContainer.Resolve<IMessageQueue>();
            RegisterMessageQueueListeners<UpdateVariableMessage>(messageQueue);
            RegisterMessageQueueListeners<CommandMessage>(messageQueue);
            RegisterMessageQueueListeners<NotifyUserMessage>(messageQueue);
            RegisterMessageQueueListeners<ExecuteScriptMessage>(messageQueue);
            RegisterMessageQueueListeners<NetworkDeviceFoundMessage>(messageQueue);
        }

        private static void RegisterMessageQueueListeners<T>(IMessageQueue messageQueue) where T : IMessageQueueMessage
        {
            var listener = IocContainer.Resolve<IList<IMessageQueueListener<T>>>().ToList();
            listener.ForEach(l => messageQueue.Subscribe<T>(l.Notify));
        }

        private class Disposer : IDisposable
        {
            private readonly CancellationTokenSource _cancellationToken;

            public Disposer(CancellationTokenSource cancellationToken)
            {
                _cancellationToken = cancellationToken;
            }

            public void Dispose()
            {
                _cancellationToken.Cancel();
                IocContainer.Dispose();
            }
        }
    }
}
