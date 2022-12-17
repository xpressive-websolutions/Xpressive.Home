namespace Xpressive.Home.Contracts.Automation
{
    public class ScheduledScript
    {
        public string Id { get; set; }
        public string ScriptId { get; set; }
        public string CronTab { get; set; }
    }
}
