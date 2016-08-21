using System.Collections.Generic;
using System.Threading.Tasks;

namespace Xpressive.Home.Contracts.Services
{
    public interface IFavoriteRadioStationService
    {
        Task<IEnumerable<FavoriteRadioStation>> GetAsync();

        Task AddAsync(TuneInRadioStation radioStation);
        Task RemoveAsync(FavoriteRadioStation favorite);
    }
}
