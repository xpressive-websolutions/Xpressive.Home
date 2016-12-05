using System;
using System.Threading;
using System.Threading.Tasks;
using Xpressive.Home.Contracts.Gateway;
using Xpressive.Home.Contracts.Messaging;
using ZWave;
using ZWave.Channel;
using ZWave.CommandClasses;

namespace Xpressive.Home.Plugins.Zwave.CommandClassHandlers
{
    internal sealed class BatteryCommandClassHandler : CommandClassHandlerTaskRunnerBase
    {
        public BatteryCommandClassHandler(IMessageQueue messageQueue)
            : base(messageQueue, CommandClass.Battery) { }

        protected override void Handle(ZwaveDevice device, Node node, ZwaveCommandQueue queue, CancellationToken cancellationToken)
        {
            Start(TimeSpan.FromDays(1), device, node, queue, cancellationToken);
        }

        protected override void Execute(ZwaveDevice device, Node node, ZwaveCommandQueue queue)
        {
            queue.AddDistinct("UpdateBatteryStatusDaily", () => UpdateBatteryStatusDaily(device, node));
        }

        private async Task UpdateBatteryStatusDaily(DeviceBase device, Node node)
        {
            var result = await node.GetCommandClass<Battery>().Get();

            if (result.Value > 85)
            {
                device.BatteryStatus = DeviceBatteryStatus.Full;
            }
            else if (result.Value > 25)
            {
                device.BatteryStatus = DeviceBatteryStatus.Good;
            }
            else
            {
                device.BatteryStatus = DeviceBatteryStatus.Low;
            }
        }
    }
}
