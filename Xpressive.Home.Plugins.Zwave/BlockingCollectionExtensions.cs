using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using log4net;

namespace Xpressive.Home.Plugins.Zwave
{
    public static class BlockingCollectionExtensions
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof (BlockingCollectionExtensions));

        public static void Add(this BlockingCollection<NodeCommand> queue, string description, Func<Task> task)
        {
            _log.Debug("Add task " + description);
            queue.Add(new NodeCommand(description, task));
        }

        public static void AddDistinct(this BlockingCollection<NodeCommand> queue, string description, Func<Task> task)
        {
            _log.Debug("Add distinct task " + description);
            queue.Add(new NodeCommand(description, task, isDistinct: true));
        }
    }
}
