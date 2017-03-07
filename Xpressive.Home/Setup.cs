using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
            var cancellationToken = new CancellationTokenSource();

            foreach (var gateway in gateways)
            {
                Task.Factory.StartNew(() => gateway.StartAsync(cancellationToken.Token), TaskCreationOptions.LongRunning);
            }

            return new Disposer(cancellationToken);
        }

        private static void RegisterMessageQueueListeners()
        {
            var updateVariableListener = IocContainer.Resolve<IList<IMessageQueueListener<UpdateVariableMessage>>>().ToList();
            var commandMessageListener = IocContainer.Resolve<IList<IMessageQueueListener<CommandMessage>>>().ToList();
            var notifyUserListener = IocContainer.Resolve<IList<IMessageQueueListener<NotifyUserMessage>>>().ToList();
            var executeScriptListener = IocContainer.Resolve<IList<IMessageQueueListener<ExecuteScriptMessage>>>().ToList();
            var messageQueue = IocContainer.Resolve<IMessageQueue>();

            updateVariableListener.ForEach(l => messageQueue.Subscribe<UpdateVariableMessage>(l.Notify));
            commandMessageListener.ForEach(l => messageQueue.Subscribe<CommandMessage>(l.Notify));
            notifyUserListener.ForEach(l => messageQueue.Subscribe<NotifyUserMessage>(l.Notify));
            executeScriptListener.ForEach(l => messageQueue.Subscribe<ExecuteScriptMessage>(l.Notify));
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
