using NPoco;

namespace Xpressive.Home.Contracts.Automation
{
    [TableName("Script")]
    [PrimaryKey("Id", AutoIncrement = false)]
    public class Script
    {
        internal string Id { get; set; }

        public string Name { get; set; }

        public string JavaScript { get; set; }
    }
}