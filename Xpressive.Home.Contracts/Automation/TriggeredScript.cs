using NPoco;

namespace Xpressive.Home.Contracts.Automation
{
    [TableName("TriggeredScript")]
    [PrimaryKey("Id", AutoIncrement = false)]
    public class TriggeredScript
    {
        public string Id { get; set; }
        public string ScriptId { get; set; }
        public string Variable { get; set; }
    }
}
