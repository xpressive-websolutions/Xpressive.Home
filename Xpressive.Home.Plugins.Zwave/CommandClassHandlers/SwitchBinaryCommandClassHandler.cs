using System;
using System.Collections.Concurrent;
using Xpressive.Home.Contracts.Messaging;
using ZWave;
using ZWave.Channel;
using ZWave.CommandClasses;

namespace Xpressive.Home.Plugins.Zwave.CommandClassHandlers
{
    internal sealed class SwitchBinaryCommandClassHandler : CommandClassHandlerTaskRunnerBase
    {
        private Node _node;
        private BlockingCollection<NodeCommand> _queue;

        public SwitchBinaryCommandClassHandler(IMessageQueue messageQueue)
            : base(messageQueue, CommandClass.SwitchBinary) { }

        protected override void Handle(ZwaveDevice device, Node node, BlockingCollection<NodeCommand> queue)
        {
            node.GetCommandClass<SwitchBinary>().Changed += (s, e) =>
            {
                HandleSwitchBinaryReport(e.Report);
            };

            _node = node;
            _queue = queue;
            Start(TimeSpan.FromMinutes(30));
        }

        protected override void Execute()
        {
            _queue.AddDistinct("Get SwitchBinary", async () =>
            {
                var result = await _node.GetCommandClass<SwitchBinary>().Get();
                HandleSwitchBinaryReport(result);
            });
        }

        private void HandleSwitchBinaryReport(SwitchBinaryReport report)
        {
            UpdateVariable(report, "SwitchBinary", report.Value);
        }
    }
}
