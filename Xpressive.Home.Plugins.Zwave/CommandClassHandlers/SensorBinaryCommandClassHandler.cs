using System;
using Xpressive.Home.Contracts.Messaging;
using ZWave;
using ZWave.Channel;
using ZWave.CommandClasses;

namespace Xpressive.Home.Plugins.Zwave.CommandClassHandlers
{
    internal sealed class SensorBinaryCommandClassHandler : CommandClassHandlerTaskRunnerBase
    {
        public SensorBinaryCommandClassHandler(IMessageQueue messageQueue)
            : base(messageQueue, CommandClass.SensorBinary) { }

        protected override void Handle(ZwaveDevice device, Node node, ZwaveCommandQueue queue)
        {
            node.GetCommandClass<SensorBinary>().Changed += (s, e) =>
            {
                HandleSensorBinaryReport(e.Report);
            };

            Start(TimeSpan.FromMinutes(30), device, node, queue);
        }

        protected override void Execute(ZwaveDevice device, Node node, ZwaveCommandQueue queue)
        {
            queue.AddDistinct("Get SensorBinary", async () =>
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
