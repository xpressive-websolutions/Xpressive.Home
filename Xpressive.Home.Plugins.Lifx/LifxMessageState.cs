using System;
using System.Text;

namespace Xpressive.Home.Plugins.Lifx
{
    internal class LifxMessageState : LifxMessage
    {
        public LifxMessageState(byte[] payload) : base(107)
        {
            Color = new HsbkColor();
            Color.Deserialize(payload);

            var power = BitConverter.ToUInt16(payload, 10);
            IsPower = power == ushort.MaxValue;

            var label = Encoding.UTF8.GetString(payload, 12, 32);
            Label = label.Trim('\0');
        }

        public HsbkColor Color { get; set; }
        public bool IsPower { get; set; }
        public string Label { get; set; }
    }
}
