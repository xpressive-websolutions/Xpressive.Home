using System;

namespace Xpressive.Home.Plugins.Lifx
{
    internal class LifxMessageStatePower : LifxMessage
    {
        public LifxMessageStatePower(byte[] payload) : base(22)
        {
            var isPower = BitConverter.ToUInt16(payload, 0);
            IsPower = isPower == ushort.MaxValue;
        }

        public bool IsPower { get; }
    }
}
