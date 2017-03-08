using Xpressive.Home.Contracts.Gateway;

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

        public bool IsSwitchBinary { get; internal set; }
    }
}
