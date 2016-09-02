using System;
using System.Threading.Tasks;
using Xpressive.Home.Contracts;
using Xpressive.Home.Contracts.Messaging;
using ZWave;
using ZWave.Channel;

namespace Xpressive.Home.Plugins.Zwave.CommandClassHandlers
{
    internal abstract class CommandClassHandlerTaskRunnerBase : CommandClassHandlerBase
    {
        private bool _isDisposing;

        protected CommandClassHandlerTaskRunnerBase(IMessageQueue messageQueue, CommandClass commandClass)
            : base(messageQueue, commandClass) { }

        protected void Start(TimeSpan interval, ZwaveDevice device, Node node, ZwaveCommandQueue queue)
        {
            Task.Run(async () =>
            {
                while (!_isDisposing)
                {
                    await TaskHelper.DelayAsync(interval, () => !_isDisposing);
                    Execute(device, node, queue);
                }
            });
        }

        protected abstract void Execute(ZwaveDevice device, Node node, ZwaveCommandQueue queue);

        protected override void Dispose(bool disposing)
        {
            _isDisposing = true;
            base.Dispose(disposing);
        }
    }
}
