namespace Xpressive.Home.Plugins.NissanLeaf
{
    internal class BatteryStatus
    {
        public double CruisingRangeAcOn { get; internal set; }
        public double CruisingRangeAcOff { get; internal set; }
        public string PluginState { get; internal set; }
        public string ChargingState { get; internal set; }
        public double Power { get; internal set; }
    }
}
