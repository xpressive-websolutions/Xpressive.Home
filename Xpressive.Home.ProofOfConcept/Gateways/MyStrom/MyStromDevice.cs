namespace Xpressive.Home.ProofOfConcept
{
    internal class MyStromDevice : DeviceBase
    {
        public MyStromDevice(string name, string ipAddress, string macAddress)
        {
            Name = name;
            Id = macAddress;
        }

        public string IpAddress { get; set; }
        public string MacAddress => Id;
    }
}