using NPoco;

namespace Xpressive.Home.Contracts.Automation
{
    [TableName("ScheduledScript")]
    [PrimaryKey("Id", AutoIncrement = false)]
    public class ScheduledScript
    {
        public string Id { get; set; }
        public string ScriptId { get; set; }
        public string CronTab { get; set; }
    }
}
