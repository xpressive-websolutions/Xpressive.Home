using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Xpressive.Home.Contracts.Messaging;
using ZWave;
using ZWave.Channel;
using ZWave.CommandClasses;

namespace Xpressive.Home.Plugins.Zwave.CommandClassHandlers
{
    internal sealed class SwitchBinaryCommandClassHandler : CommandClassHandlerBase
    {
        public SwitchBinaryCommandClassHandler(IMessageQueue messageQueue)
            : base(messageQueue, CommandClass.SwitchBinary) { }

        protected override void Handle(ZwaveDevice device, Node node, BlockingCollection<Func<Task>> queue)
        {
            node.GetCommandClass<SwitchBinary>().Changed += (s, e) =>
            {
                HandleSwitchBinaryReport(e.Report);
            };
            queue.Add(async () =>
            {
                var result = await node.GetCommandClass<SwitchBinary>().Get();
                HandleSwitchBinaryReport(result);
            });
        }

        private void HandleSwitchBinaryReport(SwitchBinaryReport report)
        {
            UpdateVariable(report, "SwitchBinary", report.Value);
        }
    }
}
