using System;
using NPoco;

namespace Xpressive.Home.Contracts.Rooms
{
    [TableName("RoomScript")]
    [PrimaryKey("Id", AutoIncrement = false)]
    public class RoomScript
    {
        public Guid Id { get; set; }
        public Guid GroupId { get; set; }
        public Guid ScriptId { get; set; }
        public string Name { get; set; }
    }
}