using System;
using Xpressive.Home.Contracts.Messaging;
using ZWave;
using ZWave.Channel;
using ZWave.CommandClasses;

namespace Xpressive.Home.Plugins.Zwave.CommandClassHandlers
{
    internal sealed class AlarmCommandClassHandler : CommandClassHandlerTaskRunnerBase
    {
        public AlarmCommandClassHandler(IMessageQueue messageQueue)
            : base(messageQueue, CommandClass.Alarm) { }

        protected override void Handle(ZwaveDevice device, Node node, ZwaveCommandQueue queue)
        {
            node.GetCommandClass<Alarm>().Changed += (s, e) =>
            {
                HandleAlarmReport(e.Report);
            };

            Start(TimeSpan.FromMinutes(10), device, node, queue);
        }

        protected override void Execute(ZwaveDevice device, Node node, ZwaveCommandQueue queue)
        {
            queue.AddDistinct("Get Alarm", async () =>
            {
                var result = await node.GetCommandClass<Alarm>().Get();
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
