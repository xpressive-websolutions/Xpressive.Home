using Xpressive.Home.Contracts.Gateway;

namespace Xpressive.Home.Plugins.MyStrom
{
    internal class MyStromDevice : DeviceBase
    {
        public MyStromDevice(string name, string ipAddress, string macAddress)
        {
            Name = name;
            Id = macAddress;
            IpAddress = ipAddress;
        }

        public string IpAddress { get; set; }
        public string MacAddress => Id;
    }
}