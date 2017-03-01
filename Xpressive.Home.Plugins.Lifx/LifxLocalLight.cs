using System;
using System.Net;

namespace Xpressive.Home.Plugins.Lifx
{
    internal sealed class LifxLocalLight
    {
        public IPEndPoint Endpoint { get; internal set; }
        public DateTime LastSeen { get; set; }
        public string Id { get; set; }
        public string Name { get; set; }
        public byte[] Mac { get; set; }
        public HsbkColor Color { get; set; }
        public bool IsOn { get; set; }
    }
}
