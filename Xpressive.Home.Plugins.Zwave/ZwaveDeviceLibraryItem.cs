using System.Collections.Generic;

namespace Xpressive.Home.Plugins.Zwave
{
    internal class ZwaveDeviceLibraryItem
    {
        public string ManufacturerId { get; set; }
        public string ProductType { get; set; }
        public string ProductId { get; set; }
        public string LibraryType { get; set; }
        public string ProtocolVersion { get; set; }
        public string ProtocolSubVersion { get; set; }
        public string ApplicationVersion { get; set; }
        public string ApplicationSubVersion { get; set; }
        public string BasicClass { get; set; }
        public string GenericClass { get; set; }
        public string SpecificClass { get; set; }
        public bool IsOptional { get; set; }
        public bool IsListening { get; set; }
        public bool IsRouting { get; set; }
        public string BeamSensor { get; set; }
        public string RfFrequency { get; set; }

        public string DeviceImage { get; set; }
        public string ProductName { get; set; }
        public string ProductCode { get; set; }
        public string BrandName { get; set; }

        public List<ZwaveDeviceLibraryItemDescription> Description { get; set; }
        public List<ZwaveDeviceLibraryItemDescription> WakeupNote { get; set; }
        public List<ZwaveDeviceLibraryItemDescription> InclusionNote { get; set; }
    }
}
