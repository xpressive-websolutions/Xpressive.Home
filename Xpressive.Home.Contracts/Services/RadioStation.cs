namespace Xpressive.Home.Contracts.Services
{
    public class RadioStation
    {
        private readonly string _url;
        private readonly string _name;

        public RadioStation(string url, string name)
        {
            _url = url;
            _name = name;
        }

        public string Name => _name;

        internal string Url => _url;
    }
}