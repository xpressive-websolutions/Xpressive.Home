using System;
using NPoco;

namespace Xpressive.Home.Contracts.Automation
{
    [TableName("Script")]
    [PrimaryKey("Id", AutoIncrement = false)]
    public class Script
    {
        public Guid Id { get; internal set; }

        public string Name { get; set; }

        public string JavaScript { get; set; }
    }
}