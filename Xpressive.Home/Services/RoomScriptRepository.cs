using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xpressive.Home.Contracts.Rooms;
using Xpressive.Home.DatabaseModel;

namespace Xpressive.Home.Services
{
    internal class RoomScriptRepository : IRoomScriptRepository
    {
        private readonly IContextFactory _contextFactory;

        public RoomScriptRepository(IContextFactory contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<IEnumerable<RoomScript>> GetAsync(Guid groupId)
        {
            return await _contextFactory.InScope(async context =>
            {
                var scripts = await context.RoomScript.Where(r => r.GroupId == groupId).ToListAsync();
                return scripts.OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase);
            });
        }

        public async Task SaveAsync(RoomScript roomScript)
        {
            await _contextFactory.InScope(async context =>
            {
                if (roomScript.Id == Guid.Empty)
                {
                    roomScript.Id = Guid.NewGuid();
                    context.RoomScript.Add(roomScript);
                }
                else
                {
                    context.RoomScript.Attach(roomScript);
                }

                await context.SaveChangesAsync();
            });

        }

        public async Task DeleteAsync(RoomScript roomScript)
        {
            await _contextFactory.InScope(async context =>
            {
                context.RoomScript.Remove(roomScript);
                await context.SaveChangesAsync();
            });
        }
    }
}