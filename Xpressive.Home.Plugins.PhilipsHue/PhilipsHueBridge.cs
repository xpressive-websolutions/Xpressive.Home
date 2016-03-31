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

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) { return false; }
            if (ReferenceEquals(this, obj)) { return true; }
            if (obj.GetType() != GetType()) { return false; }
            return Equals((PhilipsHueBridge) obj);
        }

        public override int GetHashCode()
        {
            return _id?.GetHashCode() ?? 0;
        }

        protected bool Equals(PhilipsHueBridge other)
        {
            return string.Equals(_id, other._id);
        }
    }
}