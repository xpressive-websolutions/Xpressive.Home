using Xpressive.Home.Contracts.Gateway;

namespace Xpressive.Home.Plugins.Zwave
{
    internal class ZwaveDevice : DeviceBase
    {
        public ZwaveDevice(byte nodeId)
        {
            Id = nodeId.ToString("D");
            NodeId = nodeId;
        }

        public byte NodeId { get; set; }

        public byte BasicType { get; set; }
        public byte GenericType { get; set; }
        public byte SpecificType { get; set; }

        public int ManufacturerId { get; set; }
        public int ProductType { get; set; }
        public int ProductId { get; set; }
        public string Application { get; set; }
        public string Library { get; set; }
        public string Protocol { get; set; }

        public string Manufacturer { get; set; }
        public string ProductName { get; set; }
        public string ProductDescription { get; set; }
        public string ImagePath { get; set; }

        public bool IsSwitchBinary { get; internal set; }
    }
}
