using System.Collections.Generic;

namespace Xpressive.Home.Contracts
{
    public static class QueueExtension
    {
        public static bool TryDequeue<T>(this Queue<T> queue, out T item)
        {
            if (queue.Count == 0)
            {
                item = default(T);
                return false;
            }

            item = queue.Dequeue();
            return true;
        }
    }
}
