using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Xml;

namespace Xpressive.Home.ProofOfConcept.Contracts
{
    public interface IRadioStationService
    {
        Task<IEnumerable<RadioStationCountry>> GetCountriesAsync();

        Task<IEnumerable<RadioStation>> GetStationsAsync(RadioStationCountry country);
    }

    internal sealed class RadioStationService : IRadioStationService
    {
        public async Task<IEnumerable<RadioStationCountry>> GetCountriesAsync()
        {
            var countries = new List<RadioStationCountry>();

            for (var i = 1;; i++)
            {
                var path = $"http://www.hifidelio.com/radio/{i}.xml";

                try
                {
                    var result = await GetCountriesAsync(path);
                    countries.AddRange(result);
                }
                catch(WebException e)
                {
                    if (((HttpWebResponse) e.Response).StatusCode == HttpStatusCode.NotFound)
                    {
                        break;
                    }
                }
            }

            return countries.OrderBy(c => c.GermanName).ToList();
        }

        public async Task<IEnumerable<RadioStation>> GetStationsAsync(RadioStationCountry country)
        {
            var stations = new List<RadioStation>();
            var document = await GetDocumentAsync(country.Url);

            foreach (XmlNode node in document.SelectNodes("//STATION"))
            {
                var name = node["NAME"].InnerText;
                var url = node["URL"].InnerText;
                stations.Add(new RadioStation(url, name));
            }

            return stations.OrderBy(s => s.Name).ToList();
        }

        private async Task<IEnumerable<RadioStationCountry>> GetCountriesAsync(string path)
        {
            var countries = new List<RadioStationCountry>();
            var document = await GetDocumentAsync(path);

            foreach (XmlNode node in document.SelectNodes("//COUNTRY"))
            {
                var url = node["URL"].InnerText;
                var german = node["NAME"]["DE"].InnerText;
                var english = node["NAME"]["EN"].InnerText;
                var french = node["NAME"]["FR"].InnerText;
                var country = new RadioStationCountry(url, german, english, french);
                countries.Add(country);
            }

            return countries;
        }

        private async Task<XmlDocument> GetDocumentAsync(string path)
        {
            var request = WebRequest.CreateHttp(path);
            var response = await request.GetResponseAsync();
            var stream = response.GetResponseStream();

            var document = new XmlDocument();
            document.Load(stream);
            return document;
        }
    }

    public class RadioStation
    {
        private readonly string _url;
        private readonly string _name;

        internal RadioStation(string url, string name)
        {
            _url = url;
            _name = name;
        }

        public string Name => _name;

        internal string Url => _url;
    }

    public class RadioStationCountry
    {
        private readonly string _url;
        private readonly string _germanName;
        private readonly string _englishName;
        private readonly string _frenchName;

        internal RadioStationCountry(string url, string germanName, string englishName, string frenchName)
        {
            _url = url;
            _germanName = germanName;
            _englishName = englishName;
            _frenchName = frenchName;
        }

        public string GermanName => _germanName;
        public string EnglishName => _englishName;
        public string FrenchName => _frenchName;

        internal string Url => _url;
    }
}
