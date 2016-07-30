using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Xpressive.Home.Contracts.Messaging;
using ZWave;
using ZWave.Channel;
using ZWave.CommandClasses;

namespace Xpressive.Home.Plugins.Zwave.CommandClassHandlers
{
    internal sealed class SensorBinaryCommandClassHandler : CommandClassHandlerBase
    {
        public SensorBinaryCommandClassHandler(IMessageQueue messageQueue)
            : base(messageQueue, CommandClass.SensorBinary) { }

        protected override void Handle(ZwaveDevice device, Node node, BlockingCollection<Func<Task>> queue)
        {
            node.GetCommandClass<SensorBinary>().Changed += (s, e) =>
            {
                HandleSensorBinaryReport(e.Report);
            };
            queue.Add(async () =>
            {
                var result = await node.GetCommandClass<SensorBinary>().Get();
                HandleSensorBinaryReport(result);
            });
        }

        private void HandleSensorBinaryReport(SensorBinaryReport report)
        {
            UpdateVariable(report, "SensorBinary", report.Value);
        }
    }
}
