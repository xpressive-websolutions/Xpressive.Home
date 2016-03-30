namespace Xpressive.Home.Contracts.Services
{
    public class RadioStationCountry
    {
        private readonly string _url;
        private readonly string _germanName;
        private readonly string _englishName;
        private readonly string _frenchName;

        public RadioStationCountry(string url, string germanName, string englishName, string frenchName)
        {
            _url = url;
            _germanName = germanName;
            _englishName = englishName;
            _frenchName = frenchName;
        }

        public string Url => _url;
        public string GermanName => _germanName;
        public string EnglishName => _englishName;
        public string FrenchName => _frenchName;
    }
}