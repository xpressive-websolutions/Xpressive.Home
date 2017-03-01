using System;

namespace Xpressive.Home.Plugins.Lifx
{
    internal class LifxMessageProtocolHeader
    {
        public ushort Type { get; set; }

        public LifxMessageProtocolHeader() { }

        public LifxMessageProtocolHeader(byte[] data)
        {
            Deserialize(data);
        }

        public byte[] Serialize()
        {
            var result = new byte[12];

            BitConverter.GetBytes(Type).CopyTo(result, 8);

            return result;
        }

        private void Deserialize(byte[] data)
        {
            Type = BitConverter.ToUInt16(data, 8);
        }
    }
}
