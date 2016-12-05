using System;
using System.Threading;
using Xpressive.Home.Contracts.Messaging;
using ZWave;
using ZWave.Channel;
using ZWave.CommandClasses;

namespace Xpressive.Home.Plugins.Zwave.CommandClassHandlers
{
    internal sealed class SwitchBinaryCommandClassHandler : CommandClassHandlerTaskRunnerBase
    {
        public SwitchBinaryCommandClassHandler(IMessageQueue messageQueue)
            : base(messageQueue, CommandClass.SwitchBinary) { }

        protected override void Handle(ZwaveDevice device, Node node, ZwaveCommandQueue queue, CancellationToken cancellationToken)
        {
            node.GetCommandClass<SwitchBinary>().Changed += (s, e) =>
            {
                HandleSwitchBinaryReport(e.Report);
            };

            device.IsSwitchBinary = true;
            Start(TimeSpan.FromMinutes(30), device, node, queue, cancellationToken);
        }

        protected override void Execute(ZwaveDevice device, Node node, ZwaveCommandQueue queue)
        {
            queue.AddDistinct("Get SwitchBinary", async () =>
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
