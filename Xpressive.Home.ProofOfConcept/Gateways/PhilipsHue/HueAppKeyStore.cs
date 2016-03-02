using System;
using System.Collections.Generic;

namespace Xpressive.Home.ProofOfConcept.Gateways.PhilipsHue
{
    internal class HueAppKeyStore : IHueAppKeyStore
    {
        private readonly Dictionary<string, string> _appKeys = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public HueAppKeyStore()
        {
            _appKeys.Add("", "");
        }

        public bool TryGetAppKey(string macAddress, out string appKey)
        {
            return _appKeys.TryGetValue(macAddress, out appKey);
        }

        public void AddAppKey(string macAddress, string appKey)
        {
            _appKeys[macAddress] = appKey;
        }
    }
}