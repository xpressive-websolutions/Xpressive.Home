namespace Xpressive.Home.ProofOfConcept
{
    internal class IftttDevice : DeviceBase
    {
        private readonly string _key;

        public IftttDevice(string key) : base("IFTTT Device", "IFTTT Device")
        {
            _key = key;
        }

        public string Key => _key;
    }
}