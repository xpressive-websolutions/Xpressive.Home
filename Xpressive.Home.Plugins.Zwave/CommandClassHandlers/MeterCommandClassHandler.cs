using System.Collections.Concurrent;
using Xpressive.Home.Contracts.Messaging;
using ZWave;
using ZWave.Channel;
using ZWave.CommandClasses;

namespace Xpressive.Home.Plugins.Zwave.CommandClassHandlers
{
    internal sealed class MeterCommandClassHandler : CommandClassHandlerBase
    {
        public MeterCommandClassHandler(IMessageQueue messageQueue)
            : base(messageQueue, CommandClass.Meter) { }

        protected override void Handle(ZwaveDevice device, Node node, BlockingCollection<NodeCommand> queue)
        {
            node.GetCommandClass<Meter>().Changed += (s, e) =>
            {
                HandleMeterReport(e.Report);
            };
            queue.Add("Get Meter", async () =>
            {
                var result = await node.GetCommandClass<Meter>().Get();
                HandleMeterReport(result);
            });
        }

        private void HandleMeterReport(MeterReport report)
        {
            var variable = report.Type + "Meter";
            UpdateVariable(report, variable, (double)report.Value);
            UpdateVariable(report, variable + "Unit", report.Unit);
        }
    }
}
