using System;
using Xpressive.Home.Contracts.Messaging;
using ZWave;
using ZWave.Channel;
using ZWave.CommandClasses;

namespace Xpressive.Home.Plugins.Zwave.CommandClassHandlers
{
    internal sealed class AlarmCommandClassHandler : CommandClassHandlerTaskRunnerBase
    {
        private Node _node;
        private ZwaveCommandQueue _queue;

        public AlarmCommandClassHandler(IMessageQueue messageQueue)
            : base(messageQueue, CommandClass.Alarm) { }

        protected override void Handle(ZwaveDevice device, Node node, ZwaveCommandQueue queue)
        {
            node.GetCommandClass<Alarm>().Changed += (s, e) =>
            {
                HandleAlarmReport(e.Report);
            };

            _node = node;
            _queue = queue;
            Start(TimeSpan.FromMinutes(10));
        }

        protected override void Execute()
        {
            _queue.AddDistinct("Get Alarm", async () =>
            {
                var result = await _node.GetCommandClass<Alarm>().Get();
                HandleAlarmReport(result);
            });
        }

        private void HandleAlarmReport(AlarmReport report)
        {
            var variable = report.Type + "Alarm";
            var value = report.Level != 0;
            UpdateVariable(report, variable, value);
        }
    }
}
