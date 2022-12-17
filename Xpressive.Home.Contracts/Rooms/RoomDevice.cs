namespace Xpressive.Home.Contracts.Rooms
{
    public class RoomDevice
    {
        public string Gateway { get; set; }
        public string Id { get; set; }
        public string RoomId { get; set; }

        public Room Room { get; set; }
    }
}
