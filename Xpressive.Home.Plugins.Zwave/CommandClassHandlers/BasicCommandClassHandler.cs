using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Xpressive.Home.Contracts.Messaging;
using ZWave;
using ZWave.Channel;
using ZWave.CommandClasses;

namespace Xpressive.Home.Plugins.Zwave.CommandClassHandlers
{
    internal sealed class BasicCommandClassHandler : CommandClassHandlerBase
    {
        public BasicCommandClassHandler(IMessageQueue messageQueue)
            : base(messageQueue, CommandClass.Basic) { }

        protected override void Handle(ZwaveDevice device, Node node, BlockingCollection<Func<Task>> queue)
        {
            node.GetCommandClass<Basic>().Changed += (s, e) =>
            {
                HandleBasicReport(e.Report);
            };
            queue.Add(async () =>
            {
                var result = await node.GetCommandClass<Basic>().Get();
                HandleBasicReport(result);
            });
        }

        private void HandleBasicReport(BasicReport report)
        {
            UpdateVariable(report, "Value", (int) report.Value);
        }
    }
}
