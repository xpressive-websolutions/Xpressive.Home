using System.Collections.Generic;

namespace Xpressive.Home.Contracts.Rooms
{
    public class Room
    {
        public Room()
        {
            RoomDevice = new HashSet<RoomDevice>();
        }

        public string Id { get; set; }
        public string Name { get; set; }
        public int SortOrder { get; set; }
        public string Icon { get; set; }

        public ICollection<RoomDevice> RoomDevice { get; set; }
    }
}