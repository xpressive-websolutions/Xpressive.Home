namespace Xpressive.Home.ProofOfConcept
{
    internal class MyStromDevice : DeviceBase
    {
        public MyStromDevice(string ipAddress) : base(ipAddress, ipAddress) { }

        public string IpAddress => Id;
    }
}