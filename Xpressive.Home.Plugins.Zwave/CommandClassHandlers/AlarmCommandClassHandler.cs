using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Xpressive.Home.Contracts.Messaging;
using ZWave;
using ZWave.Channel;
using ZWave.CommandClasses;

namespace Xpressive.Home.Plugins.Zwave.CommandClassHandlers
{
    internal sealed class AlarmCommandClassHandler : CommandClassHandlerBase
    {
        public AlarmCommandClassHandler(IMessageQueue messageQueue)
            : base(messageQueue, CommandClass.Alarm) { }

        protected override void Handle(ZwaveDevice device, Node node, BlockingCollection<Func<Task>> queue)
        {
            node.GetCommandClass<Alarm>().Changed += (s, e) =>
            {
                HandleAlarmReport(e.Report);
            };
            queue.Add(async () =>
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

            // TODO: zurücksetzen...
        }
    }
}
