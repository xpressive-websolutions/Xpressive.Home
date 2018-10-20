using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xpressive.Home.Contracts.Services;
using Xpressive.Home.DatabaseModel;

namespace Xpressive.Home.Services
{
    internal sealed class FavoriteRadioStationService : IFavoriteRadioStationService
    {
        private readonly XpressiveHomeContext _dbContext;

        public FavoriteRadioStationService(XpressiveHomeContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IEnumerable<Radio>> GetAsync()
        {
            var radios = await _dbContext.Radio.ToListAsync();
            return radios;
        }

        public async Task AddAsync(Radio radioStation)
        {
            await _dbContext.Radio.AddAsync(radioStation);
            await _dbContext.SaveChangesAsync();
        }

        public async Task RemoveAsync(Radio favorite)
        {
            _dbContext.Radio.Remove(favorite);
            await _dbContext.SaveChangesAsync();
        }
    }
}
