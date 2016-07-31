using System;
using Xpressive.Home.Contracts.Messaging;
using ZWave;
using ZWave.Channel;
using ZWave.CommandClasses;

namespace Xpressive.Home.Plugins.Zwave.CommandClassHandlers
{
    internal sealed class SensorAlarmCommandClassHandler : CommandClassHandlerTaskRunnerBase
    {
        private Node _node;
        private ZwaveCommandQueue _queue;

        public SensorAlarmCommandClassHandler(IMessageQueue messageQueue)
            : base(messageQueue, CommandClass.SensorAlarm) { }

        protected override void Handle(ZwaveDevice device, Node node, ZwaveCommandQueue queue)
        {
            node.GetCommandClass<SensorAlarm>().Changed += (s, e) =>
            {
                HandleSensorAlarmReport(e.Report);
            };

            _node = node;
            _queue = queue;
            Start(TimeSpan.FromMinutes(30));
        }

        protected override void Execute()
        {
            foreach (AlarmType alarmType in Enum.GetValues(typeof (AlarmType)))
            {
                _queue.AddDistinct("Get SensorAlarm " + alarmType, async () =>
                {
                    var result = await _node.GetCommandClass<SensorAlarm>().Get(alarmType);
                    HandleSensorAlarmReport(result);
                });
            }
        }

        private void HandleSensorAlarmReport(SensorAlarmReport report)
        {
            var variable = report.Type + "SensorAlarm";
            var value = report.Level != 0;
            UpdateVariable(report, variable, value);
        }
    }
}
