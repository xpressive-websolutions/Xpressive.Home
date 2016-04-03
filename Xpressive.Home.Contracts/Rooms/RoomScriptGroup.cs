using System;
using NPoco;

namespace Xpressive.Home.Contracts.Rooms
{
    [TableName("RoomScriptGroup")]
    [PrimaryKey("Id", AutoIncrement = false)]
    public class RoomScriptGroup
    {
        public Guid Id { get; set; }
        public Guid RoomId { get; set; }
        public string Name { get; set; }
        public int SortOrder { get; set; }
        public string Icon { get; set; }
    }
}