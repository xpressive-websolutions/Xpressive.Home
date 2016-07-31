using System;
using Xpressive.Home.Contracts.Messaging;
using ZWave;
using ZWave.Channel;
using ZWave.CommandClasses;

namespace Xpressive.Home.Plugins.Zwave.CommandClassHandlers
{
    internal sealed class MeterCommandClassHandler : CommandClassHandlerTaskRunnerBase
    {
        private readonly TimeSpan _interval = TimeSpan.FromMinutes(5);
        private DateTime _lastUpdate = DateTime.MinValue;

        public MeterCommandClassHandler(IMessageQueue messageQueue)
            : base(messageQueue, CommandClass.Meter) { }

        protected override void Handle(ZwaveDevice device, Node node, ZwaveCommandQueue queue)
        {
            node.GetCommandClass<Meter>().Changed += (s, e) =>
            {
                HandleMeterReport(e.Report);
            };

            Start(_interval, device, node, queue);
        }

        protected override void Execute(ZwaveDevice device, Node node, ZwaveCommandQueue queue)
        {
            queue.AddDistinct("Get Meter", async () =>
            {
                if ((DateTime.UtcNow - _lastUpdate) < _interval)
                {
                    return;
                }

                var result = await node.GetCommandClass<Meter>().Get();
                HandleMeterReport(result);
            });
        }

        private void HandleMeterReport(MeterReport report)
        {
            _lastUpdate = DateTime.UtcNow;

            var variable = report.Type + "Meter";
            UpdateVariable(report, variable, (double)report.Value);
            UpdateVariable(report, variable + "Unit", report.Unit);
        }
    }
}
