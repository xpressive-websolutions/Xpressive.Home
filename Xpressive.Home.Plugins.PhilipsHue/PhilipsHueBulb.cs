namespace Xpressive.Home.Plugins.PhilipsHue
{
    internal sealed class PhilipsHueBulb : PhilipsHueDevice
    {
        public PhilipsHueBulb(string index, string id, string name, PhilipsHueBridge bridge) : base(index, id, name, bridge) { }

        public bool IsOn { get; set; }
    }
}
