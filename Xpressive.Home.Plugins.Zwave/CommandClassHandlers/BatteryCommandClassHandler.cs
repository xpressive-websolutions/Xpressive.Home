using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using log4net;
using Xpressive.Home.Contracts.Gateway;
using Xpressive.Home.Contracts.Messaging;
using ZWave;
using ZWave.Channel;
using ZWave.CommandClasses;

namespace Xpressive.Home.Plugins.Zwave.CommandClassHandlers
{
    internal sealed class BatteryCommandClassHandler : CommandClassHandlerBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof (BasicCommandClassHandler));
        private static readonly SingleTaskRunner _taskRunner = new SingleTaskRunner();
        private bool _isDisposing;

        public BatteryCommandClassHandler(IMessageQueue messageQueue)
            : base(messageQueue, CommandClass.Battery) { }

        protected override void Handle(ZwaveDevice device, Node node, BlockingCollection<Func<Task>> queue)
        {
            _taskRunner.StartIfNotAlreadyRunning(async () =>
            {
                var lastUpdate = DateTime.MinValue;

                while (!_isDisposing)
                {
                    await Task.Delay(10);

                    if ((DateTime.UtcNow - lastUpdate).TotalDays < 1)
                    {
                        continue;
                    }

                    lastUpdate = DateTime.UtcNow;
                    queue.Add(() => UpdateBatteryStatusDaily(device, node));
                }
            });
        }

        private async Task UpdateBatteryStatusDaily(DeviceBase device, Node node)
        {
            _log.Debug($"Update battery status for node {node.NodeID}");
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

        protected override void Dispose(bool disposing)
        {
            _isDisposing = true;
            base.Dispose(disposing);
        }
    }
}
