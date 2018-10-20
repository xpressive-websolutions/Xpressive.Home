using System;

namespace Xpressive.Home.Contracts.Rooms
{
    public class RoomDevice
    {
        public string Gateway { get; set; }
        public string Id { get; set; }
        public Guid RoomId { get; set; }

        public Room Room { get; set; }
    }
}
