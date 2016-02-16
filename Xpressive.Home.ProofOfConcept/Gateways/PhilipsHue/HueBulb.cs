namespace Xpressive.Home.ProofOfConcept.Gateways.PhilipsHue
{
    internal class HueBulb : DeviceBase
    {
        private readonly HueBridge _bridge;

        public HueBulb(string id, HueBridge bridge, string name) : base(id, name)
        {
            _bridge = bridge;
        }

        public HueBridge Bridge => _bridge;
    }
}