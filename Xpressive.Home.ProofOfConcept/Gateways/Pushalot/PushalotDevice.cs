namespace Xpressive.Home.ProofOfConcept.Gateways.Pushalot
{
    internal class PushalotDevice : DeviceBase
    {
        private readonly string _key;

        public PushalotDevice(string key) : base("PushalotDevice", "PushalotDevice")
        {
            _key = key;
        }

        public string Key => _key;
    }
}