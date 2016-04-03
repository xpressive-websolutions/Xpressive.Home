using System.Collections.Generic;
using System.Threading.Tasks;

namespace Xpressive.Home.Contracts.Rooms
{
    public interface IRoomScriptGroupRepository
    {
        Task<IEnumerable<RoomScriptGroup>> GetAsync(Room room);

        Task SaveAsync(RoomScriptGroup group);

        Task DeleteAsync(RoomScriptGroup group);
    }
}