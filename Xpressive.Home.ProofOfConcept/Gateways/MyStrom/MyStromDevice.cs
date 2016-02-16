namespace Xpressive.Home.ProofOfConcept
{
    internal class MyStromDevice : DeviceBase
    {
        public MyStromDevice(string ipAddress, string macAddress) : base(macAddress, ipAddress) { }

        public string IpAddress => Name;
        public string MacAddress => Id;
    }
}