using System;
using System.Threading.Tasks;

namespace Xpressive.Home
{
    public sealed class SingleTaskRunner
    {
        private readonly object _lock = new object();
        private bool _isRunning;

        public void StartIfNotAlreadyRunning(Func<Task> action)
        {
            if (_isRunning)
            {
                return;
            }

            lock (_lock)
            {
                if (_isRunning)
                {
                    return;
                }

                _isRunning = true;
            }

            Task.Run(async () =>
            {
                await action();
                _isRunning = false;
            });
        }
    }
}