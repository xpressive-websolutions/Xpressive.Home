using System.Collections.Generic;
using System.Threading.Tasks;

namespace Xpressive.Home.Contracts.Rooms
{
    public interface IRoomRepository
    {
        Task<IEnumerable<Room>> GetAsync();
    }
}
