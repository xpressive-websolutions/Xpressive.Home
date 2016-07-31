using System;
using Xpressive.Home.Contracts.Messaging;
using ZWave;
using ZWave.Channel;
using ZWave.CommandClasses;

namespace Xpressive.Home.Plugins.Zwave.CommandClassHandlers
{
    internal sealed class SensorMultiLevelCommandClassHandler : CommandClassHandlerTaskRunnerBase
    {
        private Node _node;
        private ZwaveCommandQueue _queue;

        public SensorMultiLevelCommandClassHandler(IMessageQueue messageQueue)
            : base(messageQueue, CommandClass.SensorMultiLevel) { }

        protected override void Handle(ZwaveDevice device, Node node, ZwaveCommandQueue queue)
        {
            node.GetCommandClass<SensorMultiLevel>().Changed += (s, e) =>
            {
                HandleSensorMultiLevelReport(e.Report);
            };

            _node = node;
            _queue = queue;
            Start(TimeSpan.FromMinutes(30));
        }

        protected override void Execute()
        {
            _queue.AddDistinct("Get SensorMultiLevel", async () =>
            {
                var result = await _node.GetCommandClass<SensorMultiLevel>().Get();
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
