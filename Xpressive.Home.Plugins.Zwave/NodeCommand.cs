using System;
using System.Threading.Tasks;

namespace Xpressive.Home.Plugins.Zwave
{
    public sealed class NodeCommand
    {
        public NodeCommand(string description, Func<Task> function, bool isDistinct = false)
        {
            Description = description;
            Function = function;
            IsDistinct = isDistinct;
        }

        public string Description { get; }
        public Func<Task> Function { get; }
        public bool IsDistinct { get; }

        public override string ToString()
        {
            return Description;
        }
    }
}
