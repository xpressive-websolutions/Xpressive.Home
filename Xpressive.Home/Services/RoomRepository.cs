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
        private readonly IRoomScriptGroupRepository _roomScriptGroupRepository;
        private readonly IRoomScriptRepository _roomScriptRepository;
        private readonly IContextFactory _contextFactory;

        public RoomRepository(IRoomScriptGroupRepository roomScriptGroupRepository, IRoomScriptRepository roomScriptRepository, IContextFactory contextFactory)
        {
            _roomScriptGroupRepository = roomScriptGroupRepository;
            _roomScriptRepository = roomScriptRepository;
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

        public async Task SaveAsync(Room room)
        {
            await _contextFactory.InScope(async context =>
            {
                if (room.Id == Guid.Empty)
                {
                    room.Id = Guid.NewGuid();
                    context.Room.Add(room);
                }
                else
                {
                    context.Room.Attach(room);
                }
                await context.SaveChangesAsync();
            });
        }

        public async Task DeleteAsync(Room room)
        {
            var groups = (await _roomScriptGroupRepository.GetAsync(room)).ToList();
            var scripts = new List<RoomScript>();

            foreach (var group in groups)
            {
                scripts.AddRange(await _roomScriptRepository.GetAsync(group.Id));
            }

            await _contextFactory.InScope(async context =>
            {
                using (var transaction = context.Database.BeginTransaction())
                {
                    foreach (var script in scripts)
                    {
                        context.RoomScript.Remove(script);
                    }

                    foreach (var group in groups)
                    {
                        context.RoomScriptGroup.Remove(group);
                    }

                    context.Room.Remove(room);

                    await context.SaveChangesAsync();
                    transaction.Commit();
                }
            });
        }
    }
}
