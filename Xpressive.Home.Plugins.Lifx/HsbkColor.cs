using System;

namespace Xpressive.Home.Plugins.Lifx
{
    public sealed class HsbkColor
    {
        private double _hue;
        private double _saturation;
        private double _brightness;
        private ushort _kelvin;

        /// <summary>0..360</summary>
        public double Hue
        {
            get { return _hue; }
            set { _hue = Math.Max(0, Math.Min(360, value)); }
        }

        /// <summary>0..1</summary>
        public double Saturation
        {
            get { return _saturation; }
            set { _saturation = Math.Max(0, Math.Min(1, value)); }
        }
        
        /// <summary>0..1</summary>
        public double Brightness
        {
            get { return _brightness; }
            set { _brightness = Math.Max(0, Math.Min(1, value)); }
        }

        /// <summary>2500..9000</summary>
        public ushort Kelvin
        {
            get { return _kelvin; }
            set { _kelvin = Math.Max((ushort)2500, Math.Min((ushort)9000, value)); }
        }

        public byte[] Serialize()
        {
            var result = new byte[8];

            var hue = (ushort) (Hue / 360d * ushort.MaxValue);
            var saturation = (ushort) (Saturation * ushort.MaxValue);
            var brightness = (ushort) (Brightness * ushort.MaxValue);

            BitConverter.GetBytes(hue).CopyTo(result, 0);
            BitConverter.GetBytes(saturation).CopyTo(result, 2);
            BitConverter.GetBytes(brightness).CopyTo(result, 4);
            BitConverter.GetBytes(Kelvin).CopyTo(result, 6);

            return result;
        }

        public void Deserialize(byte[] data)
        {
            Hue = BitConverter.ToUInt16(data, 0) * 360d / ushort.MaxValue;
            Saturation = BitConverter.ToUInt16(data, 2) / (double)ushort.MaxValue;
            Brightness = BitConverter.ToUInt16(data, 4) / (double)ushort.MaxValue;
            Kelvin = BitConverter.ToUInt16(data, 6);
        }
    }
}
