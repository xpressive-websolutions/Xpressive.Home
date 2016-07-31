using System;
using System.Collections.Concurrent;
using Xpressive.Home.Contracts.Messaging;
using ZWave;
using ZWave.Channel;
using ZWave.CommandClasses;

namespace Xpressive.Home.Plugins.Zwave.CommandClassHandlers
{
    internal sealed class SensorBinaryCommandClassHandler : CommandClassHandlerTaskRunnerBase
    {
        private Node _node;
        private BlockingCollection<NodeCommand> _queue;

        public SensorBinaryCommandClassHandler(IMessageQueue messageQueue)
            : base(messageQueue, CommandClass.SensorBinary) { }

        protected override void Handle(ZwaveDevice device, Node node, BlockingCollection<NodeCommand> queue)
        {
            node.GetCommandClass<SensorBinary>().Changed += (s, e) =>
            {
                HandleSensorBinaryReport(e.Report);
            };

            _node = node;
            _queue = queue;
            Start(TimeSpan.FromMinutes(30));
        }

        protected override void Execute()
        {
            _queue.AddDistinct("Get SensorBinary", async () =>
            {
                var result = await _node.GetCommandClass<SensorBinary>().Get();
                HandleSensorBinaryReport(result);
            });
        }

        private void HandleSensorBinaryReport(SensorBinaryReport report)
        {
            UpdateVariable(report, "SensorBinary", report.Value);
        }
    }
}
