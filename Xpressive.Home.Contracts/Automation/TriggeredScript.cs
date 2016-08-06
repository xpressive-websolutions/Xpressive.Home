using System;
using NPoco;

namespace Xpressive.Home.Contracts.Automation
{
    [TableName("TriggeredScript")]
    [PrimaryKey("Id", AutoIncrement = false)]
    public class TriggeredScript
    {
        public Guid Id { get; set; }
        public Guid ScriptId { get; set; }
        public string Variable { get; set; }
    }
}
