using System.Collections.Generic;
using System.Threading.Tasks;

namespace Xpressive.Home.Contracts.Services
{
    public interface IRadioStationService
    {
        Task<IEnumerable<RadioStationCountry>> GetCountriesAsync();

        Task<IEnumerable<RadioStation>> GetStationsAsync(RadioStationCountry country);
    }
}