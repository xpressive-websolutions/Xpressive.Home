using System.Xml.Serialization;

namespace Xpressive.Home.Plugins.Denon
{
    [XmlRoot("item")]
    public class DenonDeviceDto
    {
        [XmlElement("FriendlyName")]
        public ValueDto FriendlyName { get; set; }

        [XmlElement("Power")]
        public ValueDto Power { get; set; }

        [XmlElement("InputFuncSelect")]
        public ValueDto InputFuncSelect { get; set; }

        [XmlElement("BrandId")]
        public ValueDto BrandId { get; set; }

        [XmlElement("MasterVolume")]
        public ValueDto MasterVolume { get; set; }

        [XmlElement("ModelId")]
        public ValueDto ModelId { get; set; }

        [XmlElement("Mute")]
        public ValueDto Mute { get; set; }

        [XmlElement("NetFuncSelect")]
        public ValueDto NetFuncSelect { get; set; }

        [XmlElement("RemoteMaintenance")]
        public ValueDto RemoteMaintenance { get; set; }

        [XmlElement("RenameZone")]
        public ValueDto RenameZone { get; set; }

        [XmlElement("SalesArea")]
        public ValueDto SalesArea { get; set; }

        [XmlElement("SubwooferDisplay")]
        public ValueDto SubwooferDisplay { get; set; }

        [XmlElement("TopMenuLink")]
        public ValueDto TopMenuLink { get; set; }

        [XmlElement("VideoSelect")]
        public ValueDto VideoSelect { get; set; }

        [XmlElement("VideoSelectDisp")]
        public ValueDto VideoSelectDisp { get; set; }

        [XmlElement("VideoSelectOnOff")]
        public ValueDto VideoSelectOnOff { get; set; }

        [XmlElement("VolumeDisplay")]
        public ValueDto VolumeDisplay { get; set; }

        [XmlElement("Zone2VolDisp")]
        public ValueDto Zone2VolDisp { get; set; }

        [XmlElement("ZonePower")]
        public ValueDto ZonePower { get; set; }

        [XmlElement("selectSurround")]
        public ValueDto SelectSurround { get; set; }
    }
}