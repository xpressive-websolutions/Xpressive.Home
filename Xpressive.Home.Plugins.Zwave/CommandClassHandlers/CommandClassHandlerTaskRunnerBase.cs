using System;
using System.Threading.Tasks;
using Xpressive.Home.Contracts.Messaging;
using ZWave.Channel;

namespace Xpressive.Home.Plugins.Zwave.CommandClassHandlers
{
    internal abstract class CommandClassHandlerTaskRunnerBase : CommandClassHandlerBase
    {
        private bool _isDisposing;

        protected CommandClassHandlerTaskRunnerBase(IMessageQueue messageQueue, CommandClass commandClass)
            : base(messageQueue, commandClass) { }

        protected void Start(TimeSpan interval)
        {
            Task.Run(async () =>
            {
                var lastUpdate = DateTime.MinValue;

                while (!_isDisposing)
                {
                    await Task.Delay(10);

                    if ((DateTime.UtcNow - lastUpdate) < interval)
                    {
                        continue;
                    }

                    lastUpdate = DateTime.UtcNow;
                    Execute();
                }
            });
        }

        protected abstract void Execute();

        protected override void Dispose(bool disposing)
        {
            _isDisposing = true;
            base.Dispose(disposing);
        }
    }
}
