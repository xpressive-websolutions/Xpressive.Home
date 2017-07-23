using Xpressive.Home.Contracts.Gateway;

namespace Xpressive.Home.Plugins.Workday
{
    internal class WorkdayDevice : DeviceBase
    {
        [DeviceProperty(3)]
        public string Holidays { get; set; }

        [DeviceProperty(4)]
        public string Workdays { get; set; }
    }
}
