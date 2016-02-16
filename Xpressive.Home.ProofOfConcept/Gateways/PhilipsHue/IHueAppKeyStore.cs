namespace Xpressive.Home.ProofOfConcept.Gateways.PhilipsHue
{
    internal interface IHueAppKeyStore
    {
        bool TryGetAppKey(string macAddress, out string appKey);

        void AddAppKey(string macAddress, string appKey);
    }
}