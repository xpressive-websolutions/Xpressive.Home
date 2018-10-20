using System;

namespace Xpressive.Home.Contracts.Automation
{
    public class ScheduledScript
    {
        public Guid Id { get; set; }
        public Guid ScriptId { get; set; }
        public string CronTab { get; set; }
    }
}
