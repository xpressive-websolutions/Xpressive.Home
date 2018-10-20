using System;

namespace Xpressive.Home.Contracts.Rooms
{
    public class RoomScript
    {
        public Guid Id { get; set; }
        public Guid GroupId { get; set; }
        public Guid ScriptId { get; set; }
        public string Name { get; set; }
    }
}