using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Xpressive.Home.Plugins.Zwave
{
    public static class BlockingCollectionExtensions
    {
        public static void Add(this BlockingCollection<NodeCommand> queue, string description, Func<Task> task)
        {
            queue.Add(new NodeCommand(description, task));
        }
    }
}