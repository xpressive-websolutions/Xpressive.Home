using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Xpressive.Home.Contracts.Messaging;
using ZWave;
using ZWave.Channel;
using ZWave.CommandClasses;

namespace Xpressive.Home.Plugins.Zwave.CommandClassHandlers
{
    internal sealed class SensorAlarmCommandClassHandler : CommandClassHandlerBase
    {
        public SensorAlarmCommandClassHandler(IMessageQueue messageQueue)
            : base(messageQueue, CommandClass.SensorAlarm) { }

        protected override void Handle(ZwaveDevice device, Node node, BlockingCollection<Func<Task>> queue)
        {
            node.GetCommandClass<SensorAlarm>().Changed += (s, e) =>
            {
                HandleSensorAlarmReport(e.Report);
            };
            queue.Add(async () =>
            {
                foreach (AlarmType alarmType in Enum.GetValues(typeof(AlarmType)))
                {
                    var result = await node.GetCommandClass<SensorAlarm>().Get(alarmType);
                    HandleSensorAlarmReport(result);
                }
            });
        }

        private void HandleSensorAlarmReport(SensorAlarmReport report)
        {
            var variable = report.Type + "SensorAlarm";
            var value = report.Level != 0;
            UpdateVariable(report, variable, value);

            // TODO: zurücksetzen...
        }
    }
}
