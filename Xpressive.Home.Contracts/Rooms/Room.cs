using System;
using NPoco;

namespace Xpressive.Home.Contracts.Rooms
{
    [TableName("Room")]
    [PrimaryKey("Id", AutoIncrement = false)]
    public class Room
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public int SortOrder { get; set; }
        public string Icon { get; set; }
    }
}