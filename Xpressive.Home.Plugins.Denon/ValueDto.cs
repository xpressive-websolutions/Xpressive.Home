using System.Xml.Serialization;

namespace Xpressive.Home.Plugins.Denon
{
    public class ValueDto
    {
        [XmlElement("value")]
        public string Value { get; set; }
    }
}