using NPoco;

namespace Xpressive.Home.Contracts.Automation
{
    [TableName("Script")]
    [PrimaryKey("Id", AutoIncrement = false)]
    public class Script
    {
        public string Id { get; internal set; }

        public string Name { get; set; }

        public string JavaScript { get; set; }
    }
}