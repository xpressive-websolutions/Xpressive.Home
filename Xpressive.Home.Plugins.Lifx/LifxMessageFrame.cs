using System;

namespace Xpressive.Home.Plugins.Lifx
{
    internal class LifxMessageFrame
    {
        public ushort Size { get; set; }
        public byte Origin { get; set; } = 0;
        public bool Tagged { get; set; }
        public bool Addressable { get; set; } = true;
        public ushort Protocol { get; set; } = 1024;
        public byte[] Source { get; private set; } = new byte[4];

        public LifxMessageFrame() { }

        public LifxMessageFrame(byte[] data)
        {
            Deserialize(data);
        }

        public byte[] Serialize()
        {
            var result = new byte[8];
            BitConverter.GetBytes(Size).CopyTo(result, 0);
            var protocol = BitConverter.GetBytes(Protocol);

            if (Tagged)
            {
                protocol[1] |= 1 << 5;
            }

            if (Addressable)
            {
                protocol[1] |= 1 << 4;
            }

            protocol.CopyTo(result, 2);
            Source.CopyTo(result, 4);

            return result;
        }

        private void Deserialize(byte[] data)
        {
            Size = BitConverter.ToUInt16(data, 0);
            Protocol = BitConverter.ToUInt16(data, 2);
            Array.Copy(data, 4, Source, 0, Source.Length);
        }
    }
}
