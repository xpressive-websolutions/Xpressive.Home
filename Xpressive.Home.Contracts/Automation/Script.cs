namespace Xpressive.Home.Contracts.Automation
{
    public class Script
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string JavaScript { get; set; }
        public bool IsEnabled { get; set; }
    }
}
