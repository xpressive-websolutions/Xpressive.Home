using System.Threading;
using Xpressive.Home.Contracts.Messaging;
using ZWave;
using ZWave.Channel;
using ZWave.CommandClasses;

namespace Xpressive.Home.Plugins.Zwave.CommandClassHandlers
{
    internal sealed class WakeUpCommandClassHandler : CommandClassHandlerBase
    {
        public WakeUpCommandClassHandler(IMessageQueue messageQueue)
            : base(messageQueue, CommandClass.WakeUp) { }

        protected override void Handle(ZwaveDevice device, Node node, ZwaveCommandQueue queue, CancellationToken cancellationToken)
        {
            node.GetCommandClass<WakeUp>().Changed += (s, e) =>
            {
                if (e.Report.Awake)
                {
                    queue.StartOrContinueWorker(isWakeUp: true);
                }
            };
        }
    }
}
