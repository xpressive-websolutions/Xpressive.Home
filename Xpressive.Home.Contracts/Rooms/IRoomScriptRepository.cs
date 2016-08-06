using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Xpressive.Home.Contracts.Rooms
{
    public interface IRoomScriptRepository
    {
        Task<IEnumerable<RoomScript>> GetAsync(Guid groupId);

        Task SaveAsync(RoomScript roomScript);

        Task DeleteAsync(RoomScript roomScript);
    }
}