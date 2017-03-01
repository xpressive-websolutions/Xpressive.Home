using System;

namespace Xpressive.Home.Plugins.Lifx
{
    internal class LifxMessageFrameAddress
    {
        public byte[] Target { get; private set; } = new byte[8];
        public bool AcknowledgementRequired { get; set; }
        public bool ResponseRequired { get; set; }
        public byte Sequence { get; set; }
        public bool IsLevel2 { get; set; }

        public LifxMessageFrameAddress() { }

        public LifxMessageFrameAddress(byte[] data)
        {
            Deserialize(data);
        }

        public byte[] Serialize()
        {
            var result = new byte[16];
            Target.CopyTo(result, 0);

            if (IsLevel2)
            {
                result[8] = 0x4c;
                result[9] = 0x49;
                result[10] = 0x46;
                result[11] = 0x58;
                result[12] = 0x56;
                result[13] = 0x32;
            }

            result[15] = Sequence;

            if (AcknowledgementRequired)
            {
                result[14] |= 1 << 1;
            }

            if (ResponseRequired)
            {
                result[14] |= 1 << 0;
            }

            return result;
        }

        private void Deserialize(byte[] data)
        {
            var target = new byte[8];
            Array.Copy(data, 0, target, 0, target.Length);
            Target = target;
            Sequence = data[15];
        }
    }
}
