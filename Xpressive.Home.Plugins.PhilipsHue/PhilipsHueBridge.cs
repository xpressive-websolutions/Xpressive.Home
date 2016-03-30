namespace Xpressive.Home.Plugins.PhilipsHue
{
    internal class PhilipsHueBridge
    {
        private readonly string _id;
        private readonly string _ipAddress;

        public PhilipsHueBridge(string id, string ipAddress)
        {
            _id = id;
            _ipAddress = ipAddress;
        }

        public string Id => _id;
        public string IpAddress => _ipAddress;
    }
}