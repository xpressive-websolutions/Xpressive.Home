using System;
using System.Threading.Tasks;

namespace Xpressive.Home.Plugins.Zwave
{
    public sealed class NodeCommand
    {
        public NodeCommand(string description, Func<Task> function)
        {
            Description = description;
            Function = function;
        }

        public string Description { get; }
        public Func<Task> Function { get; }
    }
}
