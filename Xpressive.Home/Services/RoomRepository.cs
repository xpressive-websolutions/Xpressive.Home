using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xpressive.Home.Contracts.Rooms;
using Xpressive.Home.DatabaseModel;

namespace Xpressive.Home.Services
{
    internal class RoomRepository : IRoomRepository
    {
        private readonly IContextFactory _contextFactory;

        public RoomRepository(IContextFactory contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<IEnumerable<Room>> GetAsync()
        {
            return await _contextFactory.InScope(async context =>
            {
                var result = await context.Room.ToListAsync();
                return result.OrderBy(r => r.SortOrder).ThenBy(r => r.Name, StringComparer.OrdinalIgnoreCase);
            });
        }
    }
}
