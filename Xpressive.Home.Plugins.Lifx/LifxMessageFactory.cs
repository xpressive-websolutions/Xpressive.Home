using System;

namespace Xpressive.Home.Plugins.Lifx
{
    internal static class LifxMessageFactory
    {
        public static LifxMessage Deserialize(byte[] message)
        {
            var frameData = new byte[8];
            var addressData = new byte[16];
            var headerData = new byte[12];

            Array.Copy(message, 0, frameData, 0, frameData.Length);
            Array.Copy(message, 8, addressData, 0, addressData.Length);
            Array.Copy(message, 24, headerData, 0, headerData.Length);

            var frame = new LifxMessageFrame(frameData);
            var address = new LifxMessageFrameAddress(addressData);
            var header = new LifxMessageProtocolHeader(headerData);

            var payloadSize = Math.Max(0, message.Length - 36);
            var payload = new byte[payloadSize];
            Array.Copy(message, 36, payload, 0, payload.Length);

            if (frame.Size != message.Length)
            {
                return null;
            }

            LifxMessage lifxMessage = null;

            switch (header.Type)
            {
                case 107:
                    lifxMessage = new LifxMessageState(payload);
                    break;
                case 22:
                case 118:
                    lifxMessage = new LifxMessageStatePower(payload);
                    break;
                case 3:
                    lifxMessage = new LifxMessageStateService(payload);
                    break;
                case 45:
                    lifxMessage = new LifxMessageAcknowledgement();
                    break;
            }

            if (lifxMessage != null)
            {
                lifxMessage.Frame = frame;
                lifxMessage.Address = address;
                lifxMessage.Header = header;
            }

            return lifxMessage;
        }
    }
}
