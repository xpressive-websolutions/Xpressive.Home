namespace Xpressive.Home.ProofOfConcept.Gateways.PhilipsHue
{
    internal class HueBulb : DeviceBase
    {
        private readonly HueBridge _bridge;

        public HueBulb(string id, HueBridge bridge, string name)
        {
            _bridge = bridge;
            Id = id;
            Name = name;
        }

        public HueBridge Bridge => _bridge;
    }
}