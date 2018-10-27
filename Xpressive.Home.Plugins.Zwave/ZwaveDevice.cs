using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Xpressive.Home.Contracts.Gateway;
using ZWave.Channel;

namespace Xpressive.Home.Plugins.Zwave
{
    internal class ZwaveDevice : DeviceBase
    {
        public ZwaveDevice(byte nodeId, uint homeId)
        {
            Id = nodeId.ToString("D");
            NodeId = nodeId;
            HomeId = homeId;
        }

        public byte NodeId { get; }

        public uint HomeId { get; }

        public SemaphoreSlim Semaphore { get; } = new SemaphoreSlim(1, 1);

        public IEnumerable<CommandClass> CommandClasses { get; set; }

        public bool IsSwitchBinary => CommandClasses?.Contains(CommandClass.SwitchBinary) ?? false;
    }
}
