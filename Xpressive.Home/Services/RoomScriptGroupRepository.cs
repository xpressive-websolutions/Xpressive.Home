using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xpressive.Home.Contracts.Rooms;
using Xpressive.Home.DatabaseModel;

namespace Xpressive.Home.Services
{
    internal class RoomScriptGroupRepository : IRoomScriptGroupRepository
    {
        private readonly IRoomScriptRepository _roomScriptRepository;
        private readonly IContextFactory _contextFactory;

        public RoomScriptGroupRepository(IRoomScriptRepository roomScriptRepository, IContextFactory contextFactory)
        {
            _roomScriptRepository = roomScriptRepository;
            _contextFactory = contextFactory;
        }

        public Task<RoomScriptGroup> GetAsync(Guid id)
        {
            return _contextFactory.InScope(async context => await context.RoomScriptGroup.FindAsync(id));
        }

        public async Task<IEnumerable<RoomScriptGroup>> GetAsync(Room room)
        {
            return await _contextFactory.InScope(async context => await context.RoomScriptGroup.Where(g => g.RoomId == room.Id).ToListAsync());
        }

        public async Task SaveAsync(RoomScriptGroup group)
        {
            await _contextFactory.InScope(async context =>
            {
                if (group.Id == Guid.Empty)
                {
                    group.Id = Guid.NewGuid();
                    context.RoomScriptGroup.Add(group);
                }
                else
                {
                    context.RoomScriptGroup.Attach(group);
                }

                await context.SaveChangesAsync();
            });
        }

        public async Task DeleteAsync(RoomScriptGroup group)
        {
            var scripts = await _roomScriptRepository.GetAsync(group.Id);

            await _contextFactory.InScope(async context =>
            {
                using (var transaction = context.Database.BeginTransaction())
                {
                    foreach (var script in scripts)
                    {
                        context.RoomScript.Remove(script);
                    }

                    context.RoomScriptGroup.Remove(group);
                    await context.SaveChangesAsync();

                    transaction.Commit();
                }
            });
        }
    }
}