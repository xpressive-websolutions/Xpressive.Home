using System;
using System.Threading.Tasks;

namespace Xpressive.Home.Contracts
{
    public static class TaskHelper
    {
        public static async Task DelayAsync(TimeSpan delay, Func<bool> predicate)
        {
            var stop = DateTime.UtcNow.Add(delay);

            while (DateTime.UtcNow < stop && predicate())
            {
                await Task.Delay(10);
            }
        }
    }
}
