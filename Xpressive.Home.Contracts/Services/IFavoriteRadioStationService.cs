using System.Collections.Generic;
using System.Threading.Tasks;

namespace Xpressive.Home.Contracts.Services
{
    public interface IFavoriteRadioStationService
    {
        Task<IEnumerable<Radio>> GetAsync();

        Task AddAsync(Radio radioStation);
        Task RemoveAsync(Radio favorite);
    }
}
