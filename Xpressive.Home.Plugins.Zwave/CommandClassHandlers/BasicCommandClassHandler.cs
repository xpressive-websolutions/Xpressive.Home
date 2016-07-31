using System;
using Xpressive.Home.Contracts.Messaging;
using ZWave;
using ZWave.Channel;
using ZWave.CommandClasses;

namespace Xpressive.Home.Plugins.Zwave.CommandClassHandlers
{
    internal sealed class BasicCommandClassHandler : CommandClassHandlerTaskRunnerBase
    {
        private Node _node;
        private ZwaveCommandQueue _queue;

        public BasicCommandClassHandler(IMessageQueue messageQueue)
            : base(messageQueue, CommandClass.Basic) { }

        protected override void Handle(ZwaveDevice device, Node node, ZwaveCommandQueue queue)
        {
            node.GetCommandClass<Basic>().Changed += (s, e) =>
            {
                HandleBasicReport(e.Report);
            };

            _node = node;
            _queue = queue;
            Start(TimeSpan.FromMinutes(30));
        }

        private void HandleBasicReport(BasicReport report)
        {
            UpdateVariable(report, "Value", (int) report.Value);
        }

        protected override void Execute()
        {
            _queue.AddDistinct("Get Basic", async () =>
            {
                var result = await _node.GetCommandClass<Basic>().Get();
                HandleBasicReport(result);
            });
        }
    }
}
