using System;
using System.Net;

namespace Xpressive.Home.Plugins.Lifx
{
    internal abstract class LifxMessage
    {
        protected LifxMessage(ushort type)
        {
            Frame = new LifxMessageFrame();
            Address = new LifxMessageFrameAddress();
            Header = new LifxMessageProtocolHeader();
            Payload = new LifxMessagePayload();

            Header.Type = type;
        }

        public LifxMessageFrame Frame { get; set; }
        public LifxMessageFrameAddress Address { get; set; }
        public LifxMessageProtocolHeader Header { get; set; }
        public LifxMessagePayload Payload { get; set; }
        public IPAddress IpAddress { get; set; }

        public byte[] Serialize()
        {
            var payload = Payload.Serialize();
            var size = 36 + payload.Length;

            Frame.Size = (ushort)size;

            var frame = Frame.Serialize();
            var address = Address.Serialize();
            var header = Header.Serialize();

            var result = new byte[size];
            frame.CopyTo(result, 0);
            address.CopyTo(result, 8);
            header.CopyTo(result, 24);
            payload.CopyTo(result, 36);

            return result;
        }
    }
}
