using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Xml;
using Xpressive.Home.Contracts.Services;

namespace Xpressive.Home.Services
{
    internal sealed class RadioStationService : IRadioStationService
    {
        public async Task<IEnumerable<RadioStationCountry>> GetCountriesAsync()
        {
            var countries = new List<RadioStationCountry>();

            for (var i = 1; ; i++)
            {
                var path = $"http://www.hifidelio.com/radio/{i}.xml";

                try
                {
                    var result = await GetCountriesAsync(path);
                    countries.AddRange(result);
                }
                catch (WebException e)
                {
                    if (((HttpWebResponse)e.Response).StatusCode == HttpStatusCode.NotFound)
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
}