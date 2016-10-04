namespace Xpressive.Home.Plugins.PhilipsHue
{
    internal sealed class PhilipsHuePresenceSensor : PhilipsHueDevice
    {
        public PhilipsHuePresenceSensor(string index, string id, string name, PhilipsHueBridge bridge) : base(index, id, name, bridge) { }

        public bool HasPresence { get; set; }
    }
}
