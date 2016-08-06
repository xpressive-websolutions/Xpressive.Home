using System;
using NPoco;

namespace Xpressive.Home.Contracts.Automation
{
    [TableName("ScheduledScript")]
    [PrimaryKey("Id", AutoIncrement = false)]
    public class ScheduledScript
    {
        public Guid Id { get; set; }
        public Guid ScriptId { get; set; }
        public string CronTab { get; set; }
    }
}
