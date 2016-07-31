using System;
using System.Collections.Concurrent;
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
        private ZwaveDevice _device;
        private Node _node;
        private BlockingCollection<NodeCommand> _queue;

        public BatteryCommandClassHandler(IMessageQueue messageQueue)
            : base(messageQueue, CommandClass.Battery) { }

        protected override void Handle(ZwaveDevice device, Node node, BlockingCollection<NodeCommand> queue)
        {
            _device = device;
            _node = node;
            _queue = queue;

            Start(TimeSpan.FromDays(1));
        }

        protected override void Execute()
        {
            _queue.AddDistinct("UpdateBatteryStatusDaily", () => UpdateBatteryStatusDaily(_device, _node));
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