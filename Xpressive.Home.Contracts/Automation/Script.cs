using System;

namespace Xpressive.Home.Contracts.Automation
{
    public class Script
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string JavaScript { get; set; }
        public bool IsEnabled { get; set; }
    }
}
