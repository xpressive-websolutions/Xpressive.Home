using RestSharp.Deserializers;

namespace Xpressive.Home.ProofOfConcept.Gateways.PhilipsHue
{
    internal class HueBridge
    {
        [DeserializeAs(Name = "id")]
        public string Id { get; set; }

        [DeserializeAs(Name = "internalipaddress")]
        public string InternalIpAddress { get; set; }

        [DeserializeAs(Name = "name")]
        public string Name { get; set; }
    }
}