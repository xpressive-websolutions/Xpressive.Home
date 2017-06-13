using Xpressive.Home.Contracts.Gateway;

namespace Xpressive.Home.Plugins.NissanLeaf
{
    internal sealed class NissanLeafDevice : DeviceBase
    {
        public NissanLeafDevice(string vin, string dcmId, string nickname, string modelYear)
        {
            Id = vin;
            DcmId = dcmId;
            Name = nickname;
            ModelYear = modelYear;
        }

        public string Vin => Id;
        public string DcmId { get; }
        public string Nickname => Name;
        public string ModelYear { get; }
        public string CustomSessionId { get; set; }

        public string ChargingState { get; set; }
        public string PluginState { get; set; }
        public double Power { get; set; }
        public double CruisingRangeAcOff { get; set; }
        public double CruisingRangeAcOn { get; set; }
    }
}
