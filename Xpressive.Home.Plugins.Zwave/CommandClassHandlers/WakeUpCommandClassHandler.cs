using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Xpressive.Home.Contracts.Messaging;
using ZWave;
using ZWave.Channel;

namespace Xpressive.Home.Plugins.Zwave.CommandClassHandlers
{
    internal sealed class WakeUpCommandClassHandler : CommandClassHandlerBase
    {
        public WakeUpCommandClassHandler(IMessageQueue messageQueue)
            : base(messageQueue, CommandClass.WakeUp) { }

        protected override void Handle(ZwaveDevice device, Node node, BlockingCollection<Func<Task>> queue)
        {
            device.IsSupportingWakeUp = true;
        }
    }
}
