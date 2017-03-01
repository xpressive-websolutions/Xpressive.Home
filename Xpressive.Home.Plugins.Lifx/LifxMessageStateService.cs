using System;

namespace Xpressive.Home.Plugins.Lifx
{
    internal class LifxMessageStateService : LifxMessage
    {
        public LifxMessageStateService(byte[] payload) : base(3)
        {
            Port = BitConverter.ToUInt32(payload, 1);
        }

        public uint Port { get; }
    }
}
