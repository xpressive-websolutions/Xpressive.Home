using System.Collections.Concurrent;
using Xpressive.Home.Contracts.Messaging;
using ZWave;
using ZWave.Channel;
using ZWave.CommandClasses;

namespace Xpressive.Home.Plugins.Zwave.CommandClassHandlers
{
    internal sealed class SensorMultiLevelCommandClassHandler : CommandClassHandlerBase
    {
        public SensorMultiLevelCommandClassHandler(IMessageQueue messageQueue)
            : base(messageQueue, CommandClass.SensorMultiLevel) { }

        protected override void Handle(ZwaveDevice device, Node node, BlockingCollection<NodeCommand> queue)
        {
            node.GetCommandClass<SensorMultiLevel>().Changed += (s, e) =>
            {
                HandleSensorMultiLevelReport(e.Report);
            };
            queue.Add("Get SensorMultiLevel", async () =>
            {
                var result = await node.GetCommandClass<SensorMultiLevel>().Get();
                HandleSensorMultiLevelReport(result);
            });
        }

        private void HandleSensorMultiLevelReport(SensorMultiLevelReport report)
        {
            var variable = report.Type + "Sensor";
            UpdateVariable(report, variable, (double)report.Value);
            UpdateVariable(report, variable + "Unit", report.Unit);
        }
    }
}
