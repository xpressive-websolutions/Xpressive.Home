using System;

namespace Xpressive.Home.Plugins.Lifx
{
    internal class LifxMessageSetColor : LifxMessage
    {
        public LifxMessageSetColor(HsbkColor color, uint durationInMs) : base(102)
        {
            Address.ResponseRequired = true;

            Payload = new LifxMessageSetColorPayload
            {
                Color = color,
                DurationInMs = durationInMs
            };
        }

        private class LifxMessageSetColorPayload : LifxMessagePayload
        {
            public HsbkColor Color { get; set; }
            public uint DurationInMs { get; set; }

            public override byte[] Serialize()
            {
                var result = new byte[13];
                Color.Serialize().CopyTo(result, 1);
                BitConverter.GetBytes(DurationInMs).CopyTo(result, 9);
                return result;
            }
        }
    }
}
