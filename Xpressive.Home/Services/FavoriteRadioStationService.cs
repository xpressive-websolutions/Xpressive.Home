using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using NPoco;
using Xpressive.Home.Contracts.Services;

namespace Xpressive.Home.Services
{
    internal sealed class FavoriteRadioStationService : IFavoriteRadioStationService
    {
        private readonly DbConnection _dbConnection;

        public FavoriteRadioStationService(DbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public async Task<IEnumerable<FavoriteRadioStation>> GetAsync()
        {
            using (var database = new Database(_dbConnection))
            {
                return await database.FetchAsync<FavoriteRadioStation>();
            }
        }

        public async Task AddAsync(TuneInRadioStation radioStation)
        {
            var favorite = new FavoriteRadioStation
            {
                Id = radioStation.Id,
                Name = radioStation.Name,
                ImageUrl = radioStation.ImageUrl
            };

            using (var database = new Database(_dbConnection))
            {
                await database.InsertAsync(favorite);
            }
        }

        public async Task RemoveAsync(FavoriteRadioStation favorite)
        {
            using (var database = new Database(_dbConnection))
            {
                await database.DeleteAsync(favorite);
            }
        }
    }
}
