using System;
using System.Threading;
using Xpressive.Home.Contracts.Messaging;
using ZWave;
using ZWave.Channel;
using ZWave.CommandClasses;

namespace Xpressive.Home.Plugins.Zwave.CommandClassHandlers
{
    internal sealed class BasicCommandClassHandler : CommandClassHandlerTaskRunnerBase
    {
        public BasicCommandClassHandler(IMessageQueue messageQueue)
            : base(messageQueue, CommandClass.Basic) { }

        protected override void Handle(ZwaveDevice device, Node node, ZwaveCommandQueue queue, CancellationToken cancellationToken)
        {
            node.GetCommandClass<Basic>().Changed += (s, e) =>
            {
                HandleBasicReport(e.Report);
            };

            Start(TimeSpan.FromMinutes(30), device, node, queue, cancellationToken);
        }

        private void HandleBasicReport(BasicReport report)
        {
            UpdateVariable(report, "Value", (double) report.Value);
        }

        protected override void Execute(ZwaveDevice device, Node node, ZwaveCommandQueue queue)
        {
            queue.AddDistinct("Get Basic", async () =>
            {
                var result = await node.GetCommandClass<Basic>().Get();
                HandleBasicReport(result);
            });
        }
    }
}
