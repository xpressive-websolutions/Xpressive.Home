using System;
using RestSharp.Deserializers;

namespace Xpressive.Home.Plugins.Lifx
{
    internal sealed class LifxHttpLight
    {
        public string Id { get; set; }
        public string Uuid { get; set; }
        public string Label { get; set; }
        public double Brightness { get; set; }
        public DateTime LastSeen { get; set; }
        public PowerState Power { get; set; }
        public LightGroup Group { get; set; }
        public LighColor Color { get; set; }

        [DeserializeAs(Name = "Connected")]
        public bool IsConnected { get; set; }

        public string GetHexColor()
        {
            var color = GetHsbkColor();
            return color.ToRgb().ToString();
        }

        public HsbkColor GetHsbkColor()
        {
            return new HsbkColor
            {
                Hue = Color.Hue,
                Saturation = Color.Saturation,
                Brightness = Brightness,
                Kelvin = Color.Kelvin
            };
        }

        public sealed class LightGroup
        {
            public string Name { get; set; }
        }

        public sealed class LighColor
        {
            public double Hue { get; set; }
            public double Saturation { get; set; }
            public int Kelvin { get; set; }
        }

        public enum PowerState
        {
            Off = 0,
            On = 1
        }
    }
}
