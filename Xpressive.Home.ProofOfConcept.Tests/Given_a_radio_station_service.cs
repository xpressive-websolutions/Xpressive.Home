using System.Linq;
using System.Threading.Tasks;
using Xpressive.Home.ProofOfConcept.Contracts;
using Xunit;
using Xunit.Abstractions;

namespace Xpressive.Home.ProofOfConcept.Tests
{
    public class Given_a_radio_station_service
    {
        private readonly ITestOutputHelper _output;

        public Given_a_radio_station_service(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task Then_list_all_countries()
        {
            var service = new RadioStationService();
            var countries = await service.GetCountriesAsync();

            foreach (var country in countries)
            {
                _output.WriteLine($"{country.EnglishName}: {country.Url}");
            }
        }

        [Fact]
        public async Task Then_list_all_swiss_stations()
        {
            var service = new RadioStationService();
            var countries = await service.GetCountriesAsync();
            var switzerland = countries.Single(c => c.EnglishName.Equals("Switzerland"));
            var stations = await service.GetStationsAsync(switzerland);

            foreach (var station in stations)
            {
                _output.WriteLine(station.Name);
            }
        }
    }
}
