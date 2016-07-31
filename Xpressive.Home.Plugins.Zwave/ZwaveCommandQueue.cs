using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Xpressive.Home.Contracts.Gateway;
using ZWave;
using ZWave.Channel.Protocol;
using ZWave.CommandClasses;

namespace Xpressive.Home.Plugins.Zwave
{
    public class ZwaveCommandQueue : IDisposable
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(ZwaveCommandQueue));
        private readonly BlockingCollection<NodeCommand> _queue;
        private readonly SemaphoreSlim _semaphore;
        private readonly ZwaveDevice _device;
        private readonly Node _node;
        private readonly object _lock = new object();
        private DateTime _lastSemaphoreRelease;
        private bool _isRunning;

        public ZwaveCommandQueue(IDevice device, Node node)
        {
            _device = (ZwaveDevice) device;
            _node = node;
            _queue = new BlockingCollection<NodeCommand>();
            _semaphore = new SemaphoreSlim(1, int.MaxValue);
        }

        public void Add(string description, Func<Task> task)
        {
            _log.Debug("Add task " + description);
            _queue.Add(new NodeCommand(description, task));
        }

        public void AddDistinct(string description, Func<Task> task)
        {
            _log.Debug("Add distinct task " + description);
            _queue.Add(new NodeCommand(description, task, isDistinct: true));
        }

        public void StartOrContinueWorker()
        {
            lock (_lock)
            {
                if (_isRunning)
                {
                    if ((DateTime.UtcNow - _lastSemaphoreRelease).TotalSeconds > 1)
                    {
                        _lastSemaphoreRelease = DateTime.UtcNow;
                        _semaphore.Release();
                    }

                    return;
                }

                _isRunning = true;
            }

            Task.Run(Run);
        }

        public void Dispose()
        {
            _queue.CompleteAdding();
            _queue.Dispose();
            _semaphore.Dispose();
        }

        private async Task Run()
        {
            while (!_queue.IsAddingCompleted)
            {
                await _semaphore.WaitAsync();

                _log.Debug("Start processing queue for node " + _node.NodeID);
                var nodeCommands = GetDistinctNodeCommands(_queue).ToList();
                var isException = false;

                foreach (var command in nodeCommands)
                {
                    _log.Debug($"Execute task {command.Description} for node {_node.NodeID}");

                    if (!await TryExecuteQueueTask(command.Function))
                    {
                        _log.Debug($"Executing task {command.Description} for node {_node.NodeID} failed.");
                        _queue.Add(command);
                        isException = true;
                    }
                }

                if (!isException && _device.IsSupportingWakeUp)
                {
                    _log.Debug($"Execute task NoMoreInformation for node {_node.NodeID}");
                    await TryExecuteQueueTask(() => _node.GetCommandClass<WakeUp>().NoMoreInformation());
                }

                _log.Debug("Finished processing queue for node " + _node.NodeID);
            }
        }

        private static IEnumerable<NodeCommand> GetDistinctNodeCommands(BlockingCollection<NodeCommand> queue)
        {
            var distinctCommands = new HashSet<string>(StringComparer.Ordinal);
            NodeCommand nodeCommand;

            while (queue.TryTake(out nodeCommand))
            {
                if (nodeCommand.IsDistinct)
                {
                    if (distinctCommands.Contains(nodeCommand.Description))
                    {
                        continue;
                    }
                    distinctCommands.Add(nodeCommand.Description);
                }

                yield return nodeCommand;
            }
        }

        private static async Task<bool> TryExecuteQueueTask(Func<Task> task)
        {
            try
            {
                await task();
            }
            catch (TransmissionException)
            {
                return false;
            }
            catch (TimeoutException)
            {
                return false;
            }
            catch (Exception e)
            {
                // todo: je nach exception...
                _log.Error(e.Message, e);
            }

            return true;
        }
    }
}
