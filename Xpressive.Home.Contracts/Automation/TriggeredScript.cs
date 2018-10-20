using System;

namespace Xpressive.Home.Contracts.Automation
{
    public class TriggeredScript
    {
        public Guid Id { get; set; }
        public Guid ScriptId { get; set; }
        public string Variable { get; set; }
    }
}
