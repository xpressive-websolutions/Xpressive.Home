using System;

namespace Xpressive.Home.Plugins.Lifx
{
    internal class LifxMessageSetPower : LifxMessage
    {
        public LifxMessageSetPower(bool isPower) : base(21)
        {
            Address.ResponseRequired = true;

            Payload = new LifxMessageSetPowerPayload
            {
                PowerLevel = (ushort)(isPower ? 65535 : 0)
            };
        }

        public LifxMessageSetPower(bool isPower, TimeSpan duration) : base(117)
        {
            Address.ResponseRequired = true;
            Address.IsLevel2 = true;
            Frame.Tagged = false;
            Frame.Addressable = true;

            Payload = new LifxMessageSetPowerWithDurationPayload
            {
                PowerLevel = (ushort)(isPower ? 65535 : 0),
                DurationInMs = (uint)Math.Max(0, Math.Min(uint.MaxValue, duration.TotalMilliseconds))
            };
        }

        private class LifxMessageSetPowerPayload : LifxMessagePayload
        {
            public ushort PowerLevel { get; set; }

            public override byte[] Serialize()
            {
                return BitConverter.GetBytes(PowerLevel);
            }
        }

        private class LifxMessageSetPowerWithDurationPayload : LifxMessagePayload
        {
            public ushort PowerLevel { get; set; }
            public uint DurationInMs { get; set; }

            public override byte[] Serialize()
            {
                var result = new byte[6];
                BitConverter.GetBytes(PowerLevel).CopyTo(result, 0);
                BitConverter.GetBytes(DurationInMs).CopyTo(result, 2);
                return result;
            }
        }
    }
}
