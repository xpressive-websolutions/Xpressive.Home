using System;
using Xpressive.Home.Contracts.Messaging;
using ZWave;
using ZWave.Channel;
using ZWave.CommandClasses;

namespace Xpressive.Home.Plugins.Zwave.CommandClassHandlers
{
    internal sealed class SensorAlarmCommandClassHandler : CommandClassHandlerTaskRunnerBase
    {
        public SensorAlarmCommandClassHandler(IMessageQueue messageQueue)
            : base(messageQueue, CommandClass.SensorAlarm) { }

        protected override void Handle(ZwaveDevice device, Node node, ZwaveCommandQueue queue)
        {
            node.GetCommandClass<SensorAlarm>().Changed += (s, e) =>
            {
                HandleSensorAlarmReport(e.Report);
            };

            Start(TimeSpan.FromMinutes(30), device, node, queue);
        }

        protected override void Execute(ZwaveDevice device, Node node, ZwaveCommandQueue queue)
        {
            foreach (AlarmType alarmType in Enum.GetValues(typeof(AlarmType)))
            {
                queue.AddDistinct("Get SensorAlarm " + alarmType, async () =>
                {
                    var result = await node.GetCommandClass<SensorAlarm>().Get(alarmType);
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
